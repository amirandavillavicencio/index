using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using Microsoft.Win32;

namespace AppPortable.Desktop;

public partial class MainWindow : Window
{
    private readonly IDocumentProcessor _processor;
    private readonly ISearchService _search;
    private string? _selectedPdf;
    private ProcessedDocument? _lastDoc;

    public ObservableCollection<SearchResultRow> Results { get; } = new();

    public MainWindow(IDocumentProcessor processor, ISearchService search)
    {
        InitializeComponent();
        _processor = processor;
        _search = search;
        ResultsList.ItemsSource = Results;
    }

    private void BtnCargar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "PDF|*.pdf",
            Title = "Seleccionar PDF"
        };

        if (dlg.ShowDialog() != true)
            return;

        _selectedPdf = dlg.FileName;
        DocName.Text = Path.GetFileName(_selectedPdf);
        DocPages.Text = "–";
        DocChunks.Text = "–";
        DocBadgeText.Text = "Pendiente";
        DocBadge.Background = System.Windows.Media.Brushes.LightYellow;
        DocCard.Visibility = Visibility.Visible;
        BtnProcesar.IsEnabled = true;

        SetStatus("PDF cargado — listo para procesar");
    }

    private async void BtnProcesar_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPdf is null)
            return;

        BtnCargar.IsEnabled = false;
        BtnProcesar.IsEnabled = false;
        ProgressCard.Visibility = Visibility.Visible;
        KpiRow.Visibility = Visibility.Collapsed;

        try
        {
            TitleStatus.Text = "Procesando...";
            ProgressLabel.Text = "Procesando documento...";
            ProgressPct.Text = "0%";
            MainProgress.Value = 0;
            StatusProgress.Value = 0;
            StatusProgress.Visibility = Visibility.Visible;

            _lastDoc = await _processor.ProcessAsync(_selectedPdf, false, CancellationToken.None);

            DocPages.Text = _lastDoc.TotalPages.ToString();
            DocChunks.Text = _lastDoc.Chunks.Count.ToString();
            DocBadgeText.Text = "Indexado";
            DocBadge.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(220, 252, 231));
            DocBadgeText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(22, 101, 52));

            KpiPages.Text = _lastDoc.TotalPages.ToString();
            KpiChunks.Text = _lastDoc.Chunks.Count.ToString();
            KpiIndex.Text = _lastDoc.Chunks.Count.ToString();
            KpiRow.Visibility = Visibility.Visible;

            MainProgress.Value = 100;
            StatusProgress.Value = 100;
            ProgressPct.Text = "100%";
            ProgressLabel.Text = "Procesamiento completado";
            TitleStatus.Text = "Listo";

            SetStatus($"Indexado OK — {_lastDoc.Chunks.Count} chunks, {_lastDoc.TotalPages} páginas");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al procesar:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            SetStatus("Error durante el procesamiento");
            TitleStatus.Text = "Error";
        }
        finally
        {
            BtnCargar.IsEnabled = true;
            BtnProcesar.IsEnabled = true;
            StatusProgress.Visibility = Visibility.Collapsed;
            ProgressCard.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
    {
        await RunSearch();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await RunSearch();
    }

    private async Task RunSearch()
    {
        var query = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(query))
            return;

        Results.Clear();
        ResultsLabel.Text = "Buscando...";
        BtnExportar.IsEnabled = false;

        try
        {
            var hits = await _search.SearchAsync(query, 50, CancellationToken.None);

            foreach (var h in hits)
            {
                Results.Add(new SearchResultRow
                {
                    DocumentName = Path.GetFileName(h.SourceFile),
                    PageNumber = h.PageStart,
                    Score = h.Score.ToString("F2"),
                    Snippet = h.Snippet?.Trim() ?? string.Empty
                });
            }

            ResultsLabel.Text = Results.Count > 0
                ? $"{Results.Count} resultado(s) para \"{query}\""
                : "Sin resultados.";

            BtnExportar.IsEnabled = Results.Count > 0;
            SetStatus($"Búsqueda: {Results.Count} resultados");
        }
        catch (Exception ex)
        {
            ResultsLabel.Text = $"Error: {ex.Message}";
        }
    }

    private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        Results.Clear();
        ResultsLabel.Text = "Escribe un término y presiona Buscar.";
        ClearDetail();
    }

    private void ResultsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not SearchResultRow row)
            return;

        DetailDocName.Text = row.DocumentName;
        DetailPageInfo.Text = $"Página {row.PageNumber}  •  Score {row.Score}";
        DetailPageNum.Text = $"Página {row.PageNumber}";
        DetailContent.Text = row.Snippet;
        BtnIrPagina.IsEnabled = true;
    }

    private void ClearDetail()
    {
        DetailDocName.Text = "–";
        DetailPageInfo.Text = "Selecciona un resultado";
        DetailPageNum.Text = "Selecciona un resultado para ver el contenido";
        DetailContent.Text = string.Empty;
        BtnIrPagina.IsEnabled = false;
    }

    private void BtnExportar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV|*.csv|JSON|*.json",
            FileName = $"resultados_{DateTime.Now:yyyyMMdd_HHmm}"
        };

        if (dlg.ShowDialog() != true)
            return;

        try
        {
            if (dlg.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var json = System.Text.Json.JsonSerializer.Serialize(
                    Results,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(dlg.FileName, json, Encoding.UTF8);
            }
            else
            {
                var lines = new List<string> { "Documento,Página,Score,Fragmento" };
                lines.AddRange(Results.Select(r =>
                    $"\"{r.DocumentName}\",{r.PageNumber},{r.Score},\"{r.Snippet.Replace("\"", "'")}\""));

                File.WriteAllLines(dlg.FileName, lines, Encoding.UTF8);
            }

            SetStatus($"Exportado → {Path.GetFileName(dlg.FileName)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al exportar:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void BtnIrPagina_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is not SearchResultRow row)
            return;

        MessageBox.Show(
            $"Página {row.PageNumber} de {row.DocumentName}\n\nIntegración con visor PDF pendiente.",
            "Ir a página",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void SetStatus(string msg)
    {
        StatusLeft.Text = $"Estado: {msg}";
    }
}

public sealed class SearchResultRow
{
    public string DocumentName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string Score { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
}