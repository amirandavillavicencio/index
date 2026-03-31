using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using AppPortable.Desktop.Commands;
using AppPortable.Desktop.Models;
using AppPortable.Infrastructure.Services;
using AppPortable.Search.Services;
using Microsoft.Win32;

namespace AppPortable.Desktop.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ILocalStorageService _storageService;
    private readonly IJsonPersistenceService _jsonPersistenceService;
    private readonly IIndexService _indexService;
    private readonly ISearchService _searchService;
    private readonly IDocumentProcessor _documentProcessor;
    private readonly Dispatcher _dispatcher;

    private string _searchText = string.Empty;
    private string _statusMessage = "Listo";
    private string _errorMessage = string.Empty;
    private string _selectedDetail = "Seleccione un documento o resultado";
    private bool _isBusy;
    private DocumentListItem? _selectedDocument;
    private SearchResult? _selectedResult;

    public ObservableCollection<DocumentListItem> Documents { get; } = [];
    public ObservableCollection<SearchResult> SearchResults { get; } = [];

    public RelayCommand LoadDocumentCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ReindexCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _storageService = new LocalStorageService();
        _jsonPersistenceService = new JsonPersistenceService();
        _indexService = new SqliteIndexService(_storageService);
        _searchService = new SqliteSearchService(_storageService, _indexService);

        _documentProcessor = new InfrastructureDocumentProcessor(
            _storageService,
            new PdfExtractionService(),
            new TesseractOcrService(),
            new ParagraphChunkingService(),
            _jsonPersistenceService,
            _indexService);

        LoadDocumentCommand = new RelayCommand(async () => await LoadDocumentAsync(), () => !IsBusy);
        SearchCommand = new RelayCommand(async () => await SearchAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(SearchText));
        ReindexCommand = new RelayCommand(async () => await ReindexAsync(), () => !IsBusy && Documents.Count > 0);

        _storageService.EnsureInitialized();
        _ = LoadExistingDocumentsAsync();
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                SearchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string SelectedDetail
    {
        get => _selectedDetail;
        private set => SetProperty(ref _selectedDetail, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                LoadDocumentCommand.RaiseCanExecuteChanged();
                SearchCommand.RaiseCanExecuteChanged();
                ReindexCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DocumentListItem? SelectedDocument
    {
        get => _selectedDocument;
        set
        {
            if (!SetProperty(ref _selectedDocument, value) || value is null)
            {
                return;
            }

            SelectedResult = null;
            SelectedDetail = BuildDocumentDetail(value.Document);
        }
    }

    public SearchResult? SelectedResult
    {
        get => _selectedResult;
        set
        {
            if (!SetProperty(ref _selectedResult, value) || value is null)
            {
                return;
            }

            SelectedDetail = BuildResultDetail(value);
        }
    }

    private async Task LoadExistingDocumentsAsync()
    {
        try
        {
            SetStatus("Inicializando...");
            await _indexService.EnsureInitializedAsync();
            await RunOnUiThreadAsync(() => Documents.Clear());

            var docs = new List<ProcessedDocument>();
            foreach (var jsonFile in Directory.EnumerateFiles(_storageService.JsonPath, "*.json"))
            {
                var doc = await _jsonPersistenceService.LoadDocumentAsync(jsonFile);
                if (doc is not null)
                {
                    docs.Add(doc);
                }
            }

            await RunOnUiThreadAsync(() =>
            {
                foreach (var document in docs.OrderByDescending(d => d.ProcessedAt))
                {
                    Documents.Add(new DocumentListItem { Document = document });
                }

                if (Documents.Count > 0)
                {
                    SelectedDocument = Documents[0];
                }
            });

            SetStatus($"Documentos cargados: {Documents.Count}");
            await RunOnUiThreadAsync(() => ReindexCommand.RaiseCanExecuteChanged());
        }
        catch (Exception ex)
        {
            SetError($"Error al cargar documentos persistidos: {ex.Message}");
        }
    }

    private async Task LoadDocumentAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            Multiselect = false,
            Title = "Seleccionar PDF"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            IsBusy = true;
            SetStatus("Procesando PDF...");

            var document = await _documentProcessor.ProcessAsync(dialog.FileName);
            var item = new DocumentListItem { Document = document };

            await RunOnUiThreadAsync(() =>
            {
                Documents.Insert(0, item);
                SelectedDocument = item;
                ReindexCommand.RaiseCanExecuteChanged();
            });

            SetStatus($"Documento procesado: {document.DocumentId}");
        }
        catch (Exception ex)
        {
            SetError($"Error procesando documento: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SearchAsync()
    {
        try
        {
            IsBusy = true;
            SetStatus($"Buscando: '{SearchText.Trim()}'...");

            var results = await _searchService.SearchAsync(SearchText.Trim(), 100);
            await RunOnUiThreadAsync(() =>
            {
                SearchResults.Clear();
                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }

                if (SearchResults.Count > 0)
                {
                    SelectedResult = SearchResults[0];
                }
                else
                {
                    SelectedResult = null;
                }
            });

            if (SearchResults.Count > 0)
            {
                SetStatus($"Resultados encontrados: {SearchResults.Count}");
            }
            else
            {
                SetStatus("Sin resultados para la consulta.");
            }
        }
        catch (Exception ex)
        {
            SetError($"Error en búsqueda: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReindexAsync()
    {
        try
        {
            IsBusy = true;
            SetStatus("Reindexando documentos...");

            await _indexService.RebuildIndexAsync(Documents.Select(d => d.Document));

            SetStatus($"Reindexación completada ({Documents.Count} documentos).");
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                await SearchAsync();
            }
        }
        catch (Exception ex)
        {
            SetError($"Error reindexando: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(string status)
    {
        RunOnUiThread(() =>
        {
            ErrorMessage = string.Empty;
            StatusMessage = status;
        });
    }

    private void SetError(string error)
    {
        RunOnUiThread(() =>
        {
            ErrorMessage = error;
            StatusMessage = "Error";
        });
    }

    private void RunOnUiThread(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _dispatcher.Invoke(action);
    }

    private Task RunOnUiThreadAsync(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return _dispatcher.InvokeAsync(action, DispatcherPriority.DataBind).Task;
    }

    private static string BuildDocumentDetail(ProcessedDocument document)
    {
        var warnings = document.Warnings.Count == 0
            ? "(sin warnings)"
            : string.Join(Environment.NewLine, document.Warnings.Select(w => $"- {w}"));

        return $"Documento seleccionado{Environment.NewLine}" +
               $"Archivo: {document.SourceFile}{Environment.NewLine}" +
               $"Document ID: {document.DocumentId}{Environment.NewLine}" +
               $"Páginas: {document.TotalPages}{Environment.NewLine}" +
               $"Procesado: {document.ProcessedAt:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
               $"Extraction summary: Native={document.ExtractionSummary.Native}, OCR={document.ExtractionSummary.Ocr}, Failed={document.ExtractionSummary.Failed}{Environment.NewLine}" +
               $"Chunks: {document.Chunks.Count}{Environment.NewLine}" +
               $"Warnings:{Environment.NewLine}{warnings}";
    }

    private static string BuildResultDetail(SearchResult result)
    {
        var text = string.IsNullOrWhiteSpace(result.ChunkText) ? "(sin texto de chunk)" : result.ChunkText;

        return $"Resultado seleccionado{Environment.NewLine}" +
               $"Archivo: {result.SourceFile}{Environment.NewLine}" +
               $"Document ID: {result.DocumentId}{Environment.NewLine}" +
               $"Chunk ID: {result.ChunkId}{Environment.NewLine}" +
               $"Páginas: {result.PageStart}-{result.PageEnd}{Environment.NewLine}" +
               $"Score: {result.Score:F4}{Environment.NewLine}" +
               $"Snippet:{Environment.NewLine}{result.Snippet}{Environment.NewLine}{Environment.NewLine}" +
               $"Texto del chunk:{Environment.NewLine}{text}";
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (_dispatcher.CheckAccess())
        {
            return SetPropertyInternal(ref field, value, propertyName);
        }

        var changed = false;
        _dispatcher.Invoke(() => changed = SetPropertyInternal(ref field, value, propertyName));
        return changed;
    }

    private bool SetPropertyInternal<T>(ref T field, T value, string? propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
