using System.Text;
using System.Xml.Linq;
using System.Diagnostics;
using System.Xml;
using System.Xml.Xsl;
using System.Reflection;
using System.Linq;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace LR2
{
    public partial class MainPage : ContentPage
    {
        private string xmlFilePath = "";
        private List<MauiProgram.Teacher> results;

        public MainPage()
        {
            InitializeComponent();
            SaxBtn.IsChecked = true;
        }

        private async void OnOpenFileButton(object sender, EventArgs e)
        {
            try
            {
                var fileResult = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select XML file",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.MacCatalyst, new[] { "public.xml" } },
                    })
                });

                if (fileResult == null) return;

                string fileName = Path.GetFileName(fileResult.FileName);
                string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                using (var sourceStream = await fileResult.OpenReadAsync())
                using (var destinationStream = File.Create(localPath))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                xmlFilePath = localPath;
                StatusLabel.Text = $"File '{fileName}' loaded successfully."; 
                
                PopulateFilters();
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error opening file: {ex.Message}";
            }
        }

        private async void PopulateFilters()
        {
            try
            {
                if (string.IsNullOrEmpty(xmlFilePath)) return;

                FullNamePicker.Items.Clear();
                FacultyPicker.Items.Clear();
                DepartmentPicker.Items.Clear();
                PositionPicker.Items.Clear();

                var doc = XDocument.Load(xmlFilePath);
                var teachers = doc.Descendants("Teacher");

                foreach (var teacher in teachers)
                {
                    AddUniqueItem(FullNamePicker, teacher.Attribute("FullName")?.Value);
                    AddUniqueItem(FacultyPicker, teacher.Element("Faculty")?.Value);
                    AddUniqueItem(DepartmentPicker, teacher.Element("Department")?.Value);
                    AddUniqueItem(PositionPicker, teacher.Element("Position")?.Value);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not read XML: {ex.Message}", "Ok");
            }
        }

        private void AddUniqueItem(Picker picker, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !picker.Items.Contains(value))
            {
                picker.Items.Add(value);
            }
        }

        private void SearchBtnHandler(object sender, EventArgs e)
        {
            StatusLabel.Text = "Searching...";
            ResultsWebView.Source = null; 
            
            MauiProgram.Teacher criteria = GetSelectedParameters();
            MauiProgram.IStrategy analyzer = GetSelectedAnalyzer();
            PerformSearch(criteria, analyzer);
        }

        private MauiProgram.Teacher GetSelectedParameters()
        {
            var criteria = new MauiProgram.Teacher();

            criteria.FullName = FullNameCheckBox.IsChecked && FullNamePicker.SelectedIndex != -1 ? FullNamePicker.SelectedItem.ToString() : null;
            criteria.Faculty = FacultyCheckBox.IsChecked && FacultyPicker.SelectedIndex != -1 ? FacultyPicker.SelectedItem.ToString() : null;
            criteria.Department = DepartmentCheckBox.IsChecked && DepartmentPicker.SelectedIndex != -1 ? DepartmentPicker.SelectedItem.ToString() : null;
            criteria.Position = PositionCheckBox.IsChecked && PositionPicker.SelectedIndex != -1 ? PositionPicker.SelectedItem.ToString() : null;

            return criteria;
        }

        private MauiProgram.IStrategy GetSelectedAnalyzer()
        {
            if (SaxBtn.IsChecked) return new MauiProgram.Sax();
            if (DomBtn.IsChecked) return new MauiProgram.Dom();
            if (LinqBtn.IsChecked) return new MauiProgram.Linq();
            return null;
        }

        private void PerformSearch(MauiProgram.Teacher criteria, MauiProgram.IStrategy analyzer)
        {
            if (analyzer == null || string.IsNullOrEmpty(xmlFilePath)) return;

            var searcher = new MauiProgram.Searcher(criteria, analyzer, xmlFilePath);
            results = searcher.SearchAlgorithm();

            if (results == null || results.Count == 0)
            {
                StatusLabel.Text = "Search complete.";
                ResultsWebView.Source = new HtmlWebViewSource { Html = "<p>Nothing found for your request.</p>" };
                return;
            }

            StatusLabel.Text = $"Found results: {results.Count}";
            ResultsWebView.Source = new HtmlWebViewSource { Html = GenerateHtmlTable(results) };
        }

        private string GenerateHtmlTable(List<MauiProgram.Teacher> teachers)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><style>");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; font-family: sans-serif; }");
            sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; vertical-align: top; }");
            sb.AppendLine("th { background-color: #f2f2f2; color: #333; }");
            sb.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
            sb.AppendLine("ul { margin: 0; padding-left: 20px; }");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Full Name</th><th>Faculty</th><th>Department</th><th>Position</th><th>Education</th></tr>");

            foreach (var t in teachers)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{t.FullName}</td>");
                sb.AppendLine($"<td>{t.Faculty}</td>");
                sb.AppendLine($"<td>{t.Department}</td>");
                sb.AppendLine($"<td>{t.Position}</td>");
                sb.AppendLine("<td>");
                if (t.Educations.Any())
                {
                    sb.AppendLine("<ul>");
                    foreach (var edu in t.Educations)
                    {
                        sb.AppendLine($"<li><b>{edu.Level}:</b> {edu.Institution} ({edu.Period})</li>");
                    }
                    sb.AppendLine("</ul>");
                }
                sb.AppendLine("</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        private void ClearFields(object sender, EventArgs e)
        {
            StatusLabel.Text = "";
            ResultsWebView.Source = null;
            
            results?.Clear();

            FullNameCheckBox.IsChecked = false;
            FacultyCheckBox.IsChecked = false;
            DepartmentCheckBox.IsChecked = false;
            PositionCheckBox.IsChecked = false;

            FullNamePicker.SelectedItem = null;
            FacultyPicker.SelectedItem = null;
            DepartmentPicker.SelectedItem = null;
            PositionPicker.SelectedItem = null;
        }
       
       private async void OnTransformToHTMLBtnClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(xmlFilePath) || !File.Exists(xmlFilePath))
            {
                await DisplayAlert("Error", "First, open an XML file to transform.", "Ok");
                return;
            }
            try
            {
                string htmlFilePath = Path.Combine(FileSystem.CacheDirectory, "TeachersList.html");
                
                var xslt = new XslCompiledTransform();
                var assembly = Assembly.GetExecutingAssembly();

                using (var stream = assembly.GetManifestResourceStream("LR2.teachers.xsl"))
                {
                    if (stream == null)
                    {
                        await DisplayAlert("Error", "Stylesheet 'teachers.xsl' not found. Check its name and 'Build Action' property.", "Ok");
                        return;
                    }
                    using (var xmlReader = XmlReader.Create(stream))
                    {
                        xslt.Load(xmlReader);
                    }
                }
                xslt.Transform(xmlFilePath, htmlFilePath);

                bool openBrowser = await DisplayAlert("Success", 
                    $"File transformed and saved successfully:\n{htmlFilePath}\n\nOpen in browser?", 
                    "Yes", 
                    "No");

                if (openBrowser)
                {
                    await Launcher.OpenAsync(new Uri($"file://{htmlFilePath}"));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Transformation Error", $"An error occurred: {ex.Message}", "Ok");
            }
        }

        private async void OnExitBtnClicked(object sender, EventArgs e)
        {
            if (await DisplayAlert("Exit", "Are you sure you want to exit?", "Yes", "No"))
            {
                Application.Current.Quit();
            }
        }
    }
}