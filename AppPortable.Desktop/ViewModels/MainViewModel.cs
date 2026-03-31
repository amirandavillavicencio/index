using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using AppPortable.Core.Services;
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

    private string _searchText = string.Empty;
    private string _statusMessage = "Listo";
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
        _storageService = new LocalStorageService();
        _jsonPersistenceService = new JsonPersistenceService();
        _indexService = new SqliteIndexService(_storageService);
        _searchService = new SqliteSearchService(_storageService, _indexService);

        _documentProcessor = new DocumentProcessor(
            _storageService,
            new PdfExtractionService(),
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
            if (SetProperty(ref _selectedDocument, value) && value is not null)
            {
                SelectedResult = null;
                SelectedDetail = $"Documento: {value.Document.DocumentId}\n" +
                                 $"Archivo: {value.Document.SourceFile}\n" +
                                 $"Páginas: {value.Document.TotalPages}\n" +
                                 $"Chunks: {value.Document.Chunks.Count}\n" +
                                 $"Native: {value.Document.ExtractionSummary.Native}, " +
                                 $"OCR: {value.Document.ExtractionSummary.Ocr}, " +
                                 $"Failed: {value.Document.ExtractionSummary.Failed}";
            }
        }
    }

    public SearchResult? SelectedResult
    {
        get => _selectedResult;
        set
        {
            if (SetProperty(ref _selectedResult, value) && value is not null)
            {
                SelectedDetail = $"Chunk: {value.ChunkId}\n" +
                                 $"Documento: {value.DocumentId}\n" +
                                 $"Páginas: {value.PageStart}-{value.PageEnd}\n" +
                                 $"Score: {value.Score:F4}\n\n" +
                                 $"Snippet:\n{value.Snippet}\n\n" +
                                 $"Texto:\n{value.ChunkText}";
            }
        }
    }

    private async Task LoadExistingDocumentsAsync()
    {
        try
        {
            await _indexService.EnsureInitializedAsync();
            Documents.Clear();

            foreach (var jsonFile in Directory.EnumerateFiles(_storageService.JsonPath, "*.json").OrderByDescending(f => f))
            {
                var doc = await _jsonPersistenceService.LoadAsync<ProcessedDocument>(jsonFile);
                if (doc is not null)
                {
                    Documents.Add(new DocumentListItem { Document = doc });
                }
            }

            StatusMessage = $"Documentos cargados: {Documents.Count}";
            ReindexCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar documentos: {ex.Message}";
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
            StatusMessage = "Procesando documento...";

            var document = await _documentProcessor.ProcessAsync(dialog.FileName);
            Documents.Insert(0, new DocumentListItem { Document = document });
            SelectedDocument = Documents[0];

            StatusMessage = $"Documento procesado: {document.DocumentId}";
            ReindexCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error procesando documento: {ex.Message}";
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            StatusMessage = "Buscando...";

            var results = await _searchService.SearchAsync(SearchText.Trim(), 100);
            SearchResults.Clear();
            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            StatusMessage = $"Resultados: {SearchResults.Count}";
            if (SearchResults.Count > 0)
            {
                SelectedResult = SearchResults[0];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error en búsqueda: {ex.Message}";
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
            StatusMessage = "Reindexando...";
            await _indexService.RebuildIndexAsync(Documents.Select(d => d.Document));
            StatusMessage = "Reindexación completada";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reindexando: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
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
