using System.Text;
using System.Xml.Linq;
using System.Diagnostics;
using System.Xml;
using System.Xml.Xsl;
using System.Reflection;

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
                editor.Text = $"File '{fileName}' loaded successfully.";
                
                PopulateFilters();
            }
            catch (Exception ex)
            {
                editor.Text = $"Error opening file: {ex.Message}";
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
            editor.Text = "";
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
                editor.Text = "Nothing found for your request.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Found results: {results.Count}\n");
            foreach (var t in results)
            {
                sb.AppendLine($"Full Name: {t.FullName}");
                sb.AppendLine($"Faculty: {t.Faculty}");
                sb.AppendLine($"Department: {t.Department}");
                sb.AppendLine($"Position: {t.Position}");
                sb.AppendLine("Education:");
                foreach(var edu in t.Educations)
                {
                    sb.AppendLine($"  - {edu.Level}: {edu.Institution} ({edu.Period})");
                }
                sb.AppendLine(new string('-', 20));
            }
            editor.Text = sb.ToString();
        }

        private void ClearFields(object sender, EventArgs e)
        {
            editor.Text = "";
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
                string htmlFilePath = Path.Combine(FileSystem.AppDataDirectory, "TeachersList.html");
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
                await DisplayAlert("Success", $"File transformed and saved successfully:\n{htmlFilePath}", "Ok");
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

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (editor.Text == null) return;
            int textLength = editor.Text.Length;
            editor.FontSize = textLength < 100 ? 18 : textLength < 500 ? 14 : 10;
        }
    }
}