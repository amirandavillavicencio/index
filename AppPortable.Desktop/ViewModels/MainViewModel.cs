using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AppPortable.Core.Interfaces;

namespace AppPortable.Desktop.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly IDocumentProcessor _processor;
        private readonly ISearchService _searchService;
        private readonly IIndexService _indexService;

        private bool _isBusy;
        private string _statusMessage = "Listo.";
        private string _searchText = string.Empty;
        private string? _selectedDocument;
        private object? _selectedResult;
        private string _detailText = string.Empty;

        public MainViewModel(
            IDocumentProcessor processor,
            ISearchService searchService,
            IIndexService indexService)
        {
            _processor     = processor     ?? throw new ArgumentNullException(nameof(processor));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _indexService  = indexService  ?? throw new ArgumentNullException(nameof(indexService));

            Documents     = new ObservableCollection<string>();
            SearchResults = new ObservableCollection<SearchResultItem>();

            LoadDocumentCommand = new RelayAsyncCommand(LoadDocumentAsync, () => !IsBusy);
            SearchCommand       = new RelayAsyncCommand(SearchAsync,       () => !IsBusy);
            ReindexCommand      = new RelayAsyncCommand(ReindexAsync,      () => !IsBusy);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<string> Documents { get; }
        public ObservableCollection<SearchResultItem> SearchResults { get; }

        public ICommand LoadDocumentCommand { get; }
        public ICommand SearchCommand       { get; }
        public ICommand ReindexCommand      { get; }

        public bool IsBusy
        {
            get => _isBusy;
            set { if (SetProperty(ref _isBusy, value)) RaiseCanExecuteChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string? SelectedDocument
        {
            get => _selectedDocument;
            set => SetProperty(ref _selectedDocument, value);
        }

        public object? SelectedResult
        {
            get => _selectedResult;
            set
            {
                if (SetProperty(ref _selectedResult, value) && value is SearchResultItem item)
                    DetailText = item.Snippet;
            }
        }

        public string DetailText
        {
            get => _detailText;
            set => SetProperty(ref _detailText, value);
        }

        private async Task LoadDocumentAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title       = "Seleccionar PDF",
                Filter      = "Archivos PDF (*.pdf)|*.pdf",
                Multiselect = false
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) != true)
                return;

            var filePath = dialog.FileName;

            await RunBusyAsync(async () =>
            {
                StatusMessage = $"Procesando {Path.GetFileName(filePath)}...";

                var result = await _processor.ProcessAsync(filePath, enableOcrFallback: true);

                await _indexService.IndexChunksAsync(result.DocumentId, result.Chunks);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var name = Path.GetFileName(filePath);
                    if (!Documents.Contains(name))
                        Documents.Add(name);
                    SelectedDocument = name;
                });

                StatusMessage = $"'{Path.GetFileName(filePath)}' listo — " +
                                $"{result.Chunks.Count} chunks indexados.";
            });
        }

        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                StatusMessage = "Escribe un término de búsqueda.";
                return;
            }

            await RunBusyAsync(async () =>
            {
                StatusMessage = $"Buscando '{SearchText}'...";

                var results = await _searchService.SearchAsync(SearchText);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchResults.Clear();
                    foreach (var r in results)
                        SearchResults.Add(new SearchResultItem
                        {
                            DocumentName = Path.GetFileName(r.SourceFile),
                            PageNumber   = r.PageStart,
                            Score        = r.Score,
                            Snippet      = r.Snippet
                        });
                });

                StatusMessage = SearchResults.Count > 0
                    ? $"{SearchResults.Count} resultado(s) encontrado(s)."
                    : "Sin resultados.";
            });
        }

        private async Task ReindexAsync()
        {
            await RunBusyAsync(async () =>
            {
                StatusMessage = "Reindexando...";
                await _indexService.RebuildIndexAsync([], CancellationToken.None);
                StatusMessage = "Reindexación completa.";
            });
        }

        private async Task RunBusyAsync(Func<Task> action)
        {
            if (IsBusy) return;
            try   { IsBusy = true; await action(); }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; System.Windows.MessageBox.Show(ex.ToString(), "Error detallado"); }
            finally { IsBusy = false; }
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;
            var d = Application.Current?.Dispatcher;
            if (d == null || d.CheckAccess())
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            else
                d.BeginInvoke(new Action(() =>
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))));
        }

        private void RaiseCanExecuteChanged()
        {
            foreach (var cmd in new[] { LoadDocumentCommand, SearchCommand, ReindexCommand })
                if (cmd is RelayAsyncCommand r) r.RaiseCanExecuteChanged();
        }

        private sealed class RelayAsyncCommand : ICommand
        {
            private readonly Func<Task> _execute;
            private readonly Func<bool>? _canExecute;

            public RelayAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
            {
                _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;
            public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
            public async void Execute(object? parameter) => await _execute();

            public void RaiseCanExecuteChanged()
            {
                var d = Application.Current?.Dispatcher;
                if (d == null || d.CheckAccess())
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                else
                    d.BeginInvoke(new Action(() =>
                        CanExecuteChanged?.Invoke(this, EventArgs.Empty)));
            }
        }
    }

    public sealed class SearchResultItem
    {
        public string DocumentName { get; init; } = string.Empty;
        public int    PageNumber   { get; init; }
        public double Score        { get; init; }
        public string Snippet      { get; init; } = string.Empty;
    }
}




