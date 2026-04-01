using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AppPortable.Desktop.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private string _statusMessage = "Listo.";
        private string _searchText = string.Empty;
        private object? _selectedDocument;
        private object? _selectedResult;
        private string _detailText = string.Empty;

        public MainViewModel()
        {
            Documents = new ObservableCollection<object>();
            SearchResults = new ObservableCollection<object>();

            LoadDocumentCommand = new RelayAsyncCommand(LoadDocumentAsync, () => !IsBusy);
            SearchCommand = new RelayAsyncCommand(SearchAsync, () => !IsBusy);
            ReindexCommand = new RelayAsyncCommand(ReindexAsync, () => !IsBusy);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<object> Documents { get; }

        public ObservableCollection<object> SearchResults { get; }

        public ICommand LoadDocumentCommand { get; }

        public ICommand SearchCommand { get; }

        public ICommand ReindexCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
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

        public object? SelectedDocument
        {
            get => _selectedDocument;
            set => SetProperty(ref _selectedDocument, value);
        }

        public object? SelectedResult
        {
            get => _selectedResult;
            set => SetProperty(ref _selectedResult, value);
        }

        public string DetailText
        {
            get => _detailText;
            set => SetProperty(ref _detailText, value);
        }

        private async Task LoadDocumentAsync()
        {
            await RunBusyAsync(async () =>
            {
                StatusMessage = "Cargando documento...";
                await Task.CompletedTask;
                StatusMessage = "Documento cargado.";
            });
        }

        private async Task SearchAsync()
        {
            await RunBusyAsync(async () =>
            {
                StatusMessage = "Buscando...";
                await Task.CompletedTask;
                StatusMessage = "Búsqueda finalizada.";
            });
        }

        private async Task ReindexAsync()
        {
            await RunBusyAsync(async () =>
            {
                StatusMessage = "Reindexando...";
                await Task.CompletedTask;
                StatusMessage = "Reindexación finalizada.";
            });
        }

        private async Task RunBusyAsync(Func<Task> action)
        {
            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;
                await action();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
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
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher == null || dispatcher.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return;
            }

            dispatcher.BeginInvoke(new Action(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }));
        }

        private void RaiseCanExecuteChanged()
        {
            if (LoadDocumentCommand is RelayAsyncCommand loadCommand)
            {
                loadCommand.RaiseCanExecuteChanged();
            }

            if (SearchCommand is RelayAsyncCommand searchCommand)
            {
                searchCommand.RaiseCanExecuteChanged();
            }

            if (ReindexCommand is RelayAsyncCommand reindexCommand)
            {
                reindexCommand.RaiseCanExecuteChanged();
            }
        }

        private sealed class RelayAsyncCommand : ICommand
        {
            private readonly Func<Task> _execute;
            private readonly Func<bool>? _canExecute;

            public RelayAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

            public async void Execute(object? parameter) => await _execute();

            public void RaiseCanExecuteChanged()
            {
                var dispatcher = Application.Current?.Dispatcher;

                if (dispatcher == null || dispatcher.CheckAccess())
                {
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }

                dispatcher.BeginInvoke(new Action(() =>
                {
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }));
            }
        }
    }
}