using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    private SearchResultRow? _selectedResult;

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
        _lastDoc = null;
        _selectedResult = null;

        BtnProcesar.IsEnabled = true;
        BtnIrPagina.IsEnabled = false;
        BtnExportar.IsEnabled = false;
        BtnExportarDocumentoOcrMd.IsEnabled = false;

        Results.Clear();

        DetailDocName.Text = Path.GetFileName(_selectedPdf);
        DetailPageInfo.Text = "PDF cargado. Falta procesar.";
        DetailPageNum.Text = "Sin contenido cargado";
        DetailContent.Text = "";

        ResultsLabel.Text = "PDF cargado. Procesa e indexa para poder buscar.";
        TitleStatus.Text = "PDF cargado";
        StatusLeft.Text = $"Estado: PDF seleccionado • {Path.GetFileName(_selectedPdf)}";
        StatusRight.Text = "Pendiente";

        DocCard.Visibility = Visibility.Visible;
        ProgressCard.Visibility = Visibility.Collapsed;
        KpiRow.Visibility = Visibility.Collapsed;

        DocName.Text = Path.GetFileName(_selectedPdf);
        DocPages.Text = "–";
        DocChunks.Text = "–";
        DocBadgeText.Text = "Pendiente";
    }

    private async void BtnProcesar_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPdf is null)
            return;

        try
        {
            BtnProcesar.IsEnabled = false;
            BtnExportar.IsEnabled = false;
            BtnIrPagina.IsEnabled = false;
            BtnExportarDocumentoOcrMd.IsEnabled = false;

            ProgressCard.Visibility = Visibility.Visible;
            MainProgress.Value = 0;
            ProgressPct.Text = "Procesando...";
            ProgressLabel.Text = "Extrayendo texto y ejecutando OCR";
            StatusRight.Text = "Procesando";
            TitleStatus.Text = "Procesando";
            DocBadgeText.Text = "Procesando";

            _lastDoc = await _processor.ProcessAsync(_selectedPdf, true, CancellationToken.None);

            MainProgress.Value = 100;
            ProgressPct.Text = "100%";
            ProgressLabel.Text = "Proceso completado";

            var pageCount = _lastDoc.Pages?.Count ?? 0;
            var chunkCount = _lastDoc.Chunks?.Count ?? 0;

            DocPages.Text = pageCount.ToString();
            DocChunks.Text = chunkCount.ToString();
            DocBadgeText.Text = "Procesado";

            KpiRow.Visibility = Visibility.Visible;
            KpiPages.Text = pageCount.ToString();
            KpiChunks.Text = chunkCount.ToString();
            KpiIndex.Text = "OK";

            ResultsLabel.Text = "Procesamiento completado. Ya puedes buscar.";
            StatusLeft.Text = $"Estado: procesado • {Path.GetFileName(_selectedPdf)}";
            StatusRight.Text = "Listo";
            TitleStatus.Text = "Listo";

            DetailDocName.Text = Path.GetFileName(_selectedPdf);
            DetailPageInfo.Text = $"Documento procesado • {pageCount} páginas";
            DetailPageNum.Text = "Selecciona un resultado para ver el contenido";
            DetailContent.Text = "";

            BtnExportarDocumentoOcrMd.IsEnabled = true;
            BtnExportar.IsEnabled = Results.Count > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al procesar el PDF:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            StatusRight.Text = "Error";
            TitleStatus.Text = "Error";
            DocBadgeText.Text = "Error";
        }
        finally
        {
            BtnProcesar.IsEnabled = true;
        }
    }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
    {
        var query = SearchBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
            return;

        try
        {
            Results.Clear();
            _selectedResult = null;
            BtnIrPagina.IsEnabled = false;
            BtnExportar.IsEnabled = false;

            ResultsLabel.Text = "Buscando...";
            StatusRight.Text = "Buscando";
            TitleStatus.Text = "Buscando";

            var hits = await _search.SearchAsync(query, 50, CancellationToken.None);

            foreach (var h in hits)
            {
                Results.Add(new SearchResultRow
                {
                    DocumentName = Path.GetFileName(h.SourceFile),
                    PageNumber = h.PageStart,
                    Score = h.Score.ToString("F2"),
                    Snippet = h.Snippet ?? ""
                });
            }

            ResultsLabel.Text = $"Resultados: {Results.Count}";
            StatusRight.Text = "Listo";
            TitleStatus.Text = "Listo";
            BtnExportar.IsEnabled = Results.Count > 0;

            if (Results.Count == 0)
            {
                DetailPageInfo.Text = "Sin resultados";
                DetailPageNum.Text = "No se encontraron coincidencias";
                DetailContent.Text = "";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error en la búsqueda:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            ResultsLabel.Text = "Error al buscar.";
            StatusRight.Text = "Error";
            TitleStatus.Text = "Error";
        }
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            BtnBuscar_Click(sender, e);
    }

    private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
        Results.Clear();
        _selectedResult = null;

        BtnIrPagina.IsEnabled = false;
        BtnExportar.IsEnabled = false;

        ResultsLabel.Text = "Escribe un término y presiona Buscar.";
        DetailPageInfo.Text = "Selecciona un resultado";
        DetailPageNum.Text = "Selecciona un resultado para ver el contenido";
        DetailContent.Text = "";
    }

    private void ResultsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not SearchResultRow row)
            return;

        _selectedResult = row;

        var pageText = _lastDoc?.Pages?
            .FirstOrDefault(p => p.PageNumber == row.PageNumber)?
            .Text;

        DetailDocName.Text = row.DocumentName;
        DetailPageInfo.Text = $"Página {row.PageNumber}";
        DetailPageNum.Text = $"Página {row.PageNumber}";
        DetailContent.Text = string.IsNullOrWhiteSpace(pageText)
            ? row.Snippet
            : pageText.Trim();

        BtnIrPagina.IsEnabled = true;
        BtnExportar.IsEnabled = Results.Count > 0;
    }

    private void BtnExportar_Click(object sender, RoutedEventArgs e)
    {
        if (Results.Count == 0)
        {
            MessageBox.Show("No hay resultados para exportar.");
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "Markdown|*.md|Texto|*.txt",
            FileName = "resultados_busqueda.md"
        };

        if (dlg.ShowDialog() != true)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("# Resultados de búsqueda");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            sb.AppendLine($"**Consulta:** {SearchBox.Text.Trim()}");
            sb.AppendLine();
        }

        foreach (var item in Results)
        {
            sb.AppendLine($"## {item.DocumentName}");
            sb.AppendLine($"- Página: {item.PageNumber}");
            sb.AppendLine($"- Score: {item.Score}");
            sb.AppendLine();
            sb.AppendLine(item.Snippet);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(false));
        MessageBox.Show("Resultados exportados correctamente.");
    }

    private void BtnIrPagina_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedResult is null)
        {
            MessageBox.Show("Selecciona un resultado primero.");
            return;
        }

        var pageText = _lastDoc?.Pages?
            .FirstOrDefault(p => p.PageNumber == _selectedResult.PageNumber)?
            .Text;

        DetailDocName.Text = _selectedResult.DocumentName;
        DetailPageInfo.Text = $"Página {_selectedResult.PageNumber}";
        DetailPageNum.Text = $"Página {_selectedResult.PageNumber}";
        DetailContent.Text = string.IsNullOrWhiteSpace(pageText)
            ? _selectedResult.Snippet
            : pageText.Trim();
    }

    private void BtnExportarDocumentoOcrMd_Click(object sender, RoutedEventArgs e)
    {
        if (_lastDoc is null)
        {
            MessageBox.Show("Procesa un PDF primero.");
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "Markdown|*.md",
            FileName = "documento_ocr.md"
        };

        if (dlg.ShowDialog() != true)
            return;

        ExportDocumentoOcr(dlg.FileName, _lastDoc);
        MessageBox.Show("OCR completo exportado correctamente.");
    }

    private void ExportDocumentoOcr(string path, ProcessedDocument doc)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {Path.GetFileName(doc.SourceFile)}");
        sb.AppendLine();

        sb.AppendLine("## Índice");
        sb.AppendLine();

        foreach (var p in doc.Pages)
            sb.AppendLine($"{p.PageNumber}. [Página {p.PageNumber}](#pagina-{p.PageNumber})");

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var p in doc.Pages)
        {
            sb.AppendLine($"## Página {p.PageNumber}");
            sb.AppendLine($"<a id=\"pagina-{p.PageNumber}\"></a>");
            sb.AppendLine();

            sb.AppendLine("```text");
            sb.AppendLine(string.IsNullOrWhiteSpace(p.Text) ? "[Sin texto]" : p.Text);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }
}

public sealed class SearchResultRow
{
    public string DocumentName { get; set; } = "";
    public int PageNumber { get; set; }
    public string Score { get; set; } = "";
    public string Snippet { get; set; } = "";
}