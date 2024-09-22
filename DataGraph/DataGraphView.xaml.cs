using OfficeOpenXml;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace DataGraph
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DataGraphView : Window
    {

        private PlotModel _plotModel;
        private Dictionary<string, List<DataPoint>> _stateData;

        public DataGraphView()
        {
            InitializeComponent();
            _plotModel = new PlotModel { Title = "Unemployment Data" };

            var dateAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "yyyy",  // Show only the year in X-Axis
                Title = "Year",
                IntervalType = DateTimeIntervalType.Years,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,  // Enable zooming /changing scale on the Y-axis
                IsPanEnabled = true
            };
            _plotModel.Axes.Add(dateAxis);

            // Setup the Y-Axis for unemployment rate
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Unemployment Rate (%)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,  // Enable zooming /changing scale on the Y-axis
                IsPanEnabled = true
            };
            _plotModel.Axes.Add(valueAxis);
            
                
           PlotView.Model = _plotModel;
           LoadStateList();
        }

        private void LoadStateList()
        {
            // Load state names from the Excel file
            _stateData = LoadExcelData("UnemploymentData.xlsx");
            StateSelector.ItemsSource = _stateData.Keys.ToList();
        }


        private Dictionary<string, List<DataPoint>> LoadExcelData(string path)
        {
            // Set the license context for EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var stateData = new Dictionary<string, List<DataPoint>>();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(path)))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // Assuming the first worksheet
                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    // Loop through each row starting from the second row (assuming the first row is header)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        // Read state name from the first column
                        string state = worksheet.Cells[row, 1].Text;

                       

                        // If the state name is null or empty, skip this row
                        if (string.IsNullOrWhiteSpace(state))
                        {
                            continue;
                        }

                        // Prepare a list to store unemployment data points for this state
                        List<DataPoint> points = new List<DataPoint>();

                        // Iterate over columns to read unemployment data (starting from the 2nd column)
                        for (int col = 2; col <= colCount; col++)
                        {

                            double year = DateTime.ParseExact(worksheet.Cells[1, col].Text, "yyyy-MM", null).ToOADate(); 

                            if (double.TryParse(worksheet.Cells[row, col].Text, out double unemploymentRate))
                            {
                                // Create a new data point for the year and unemployment rate
                                points.Add(new DataPoint(year, unemploymentRate));
                            }
                        }

                        // Add the state and its corresponding data points to the dictionary
                        stateData[state] = points;
                    }
                }
            }
            catch (Exception ex)
            {
                // Display error message in case of an exception
                MessageBox.Show($"Error loading Excel data: {ex.Message}");
            }

            return stateData;
        }
        private void PlotStateData(string state)
        {
            _plotModel.Series.Clear();
            LineSeries series = new LineSeries
            {
                Title = state,
                ItemsSource = _stateData[state]
            };

            _plotModel.Series.Add(series);
            _plotModel.InvalidatePlot(true);
        }

        private void SaveGraphButton_Click(object sender, RoutedEventArgs e)
        {
            var pngExporter = new OxyPlot.SkiaSharp.PngExporter { Width = 600, Height = 400 };
            using (var stream = File.Create("UnemploymentGraph.png"))
            {
                pngExporter.Export(_plotModel, stream);
            }

            MessageBox.Show("Graph saved as UnemploymentGraph.png");
        }

        private void StateSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string selectedState = StateSelector.SelectedItem?.ToString();
            if (selectedState != null && _stateData.ContainsKey(selectedState))
            {
                PlotStateData(selectedState);
            }
        }
    }
}
