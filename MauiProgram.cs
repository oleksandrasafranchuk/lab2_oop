using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace LR2
{
    public static class MauiProgram
    {
        
        public class Education
        {
            public string Level { get; set; }
            public string Institution { get; set; }
            public string Period { get; set; }
        }

        public class Teacher
        {
            public string FullName { get; set; }
            public string Faculty { get; set; }
            public string Department { get; set; }
            public string Position { get; set; }
            public List<Education> Educations { get; set; } = new List<Education>();
        }

        private static string xmlFilePath;

        public interface IStrategy
        {
            List<Teacher> Search(Teacher criteria);
        }

        public class Searcher
        {
            private Teacher searchCriteria;
            private IStrategy strategy;

            public Searcher(Teacher criteria, IStrategy str, string path)
            {
                searchCriteria = criteria;
                strategy = str;
                xmlFilePath = path;
            }

            public List<Teacher> SearchAlgorithm()
            {
                return strategy?.Search(searchCriteria) ?? new List<Teacher>();
            }
        }

     

        public class Sax : IStrategy
        {
            public List<Teacher> Search(Teacher criteria)
            {
                var results = new List<Teacher>();
                Teacher currentTeacher = null;

                try
                {
                    using (XmlReader reader = XmlReader.Create(xmlFilePath))
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                if (reader.Name == "Teacher")
                                {
                                    currentTeacher = new Teacher
                                    {
                                        FullName = reader.GetAttribute("FullName")
                                    };
                                }
                                else if (currentTeacher != null)
                                {
                                    switch (reader.Name)
                                    {
                                        case "Faculty":
                                            currentTeacher.Faculty = reader.ReadElementContentAsString();
                                            break;
                                        case "Department":
                                            currentTeacher.Department = reader.ReadElementContentAsString();
                                            break;
                                        case "Position":
                                            currentTeacher.Position = reader.ReadElementContentAsString();
                                            break;
                                        case "Education":
                                            var education = new Education { Level = reader.GetAttribute("Level") };
                                            if (reader.ReadToFollowing("Institution"))
                                            {
                                                education.Institution = reader.ReadElementContentAsString();
                                                if(reader.ReadToFollowing("Period"))
                                                {
                                                   education.Period = reader.ReadElementContentAsString();
                                                }
                                            }
                                            currentTeacher.Educations.Add(education);
                                            break;
                                    }
                                }
                            }
                            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Teacher")
                            {
                                bool isMatch =
                                    (criteria.FullName == null || currentTeacher.FullName == criteria.FullName) &&
                                    (criteria.Faculty == null || currentTeacher.Faculty == criteria.Faculty) &&
                                    (criteria.Department == null || currentTeacher.Department == criteria.Department) &&
                                    (criteria.Position == null || currentTeacher.Position == criteria.Position);

                                if (isMatch)
                                {
                                    results.Add(currentTeacher);
                                }
                                currentTeacher = null;
                            }
                        }
                    }
                }
                catch (Exception ex) { Debug.WriteLine($"SAX Error: {ex.Message}"); }
                return results;
            }
        }

        public class Dom : IStrategy
        {
            public List<Teacher> Search(Teacher criteria)
            {
                var results = new List<Teacher>();
                var doc = new XmlDocument();
                try { doc.Load(xmlFilePath); } catch { return null; }

                var nodes = doc.SelectNodes("//Teacher");
                foreach (XmlNode node in nodes)
                {
                    var teacher = new Teacher
                    {
                        FullName = node.Attributes["FullName"]?.Value,
                        Faculty = node.SelectSingleNode("Faculty")?.InnerText,
                        Department = node.SelectSingleNode("Department")?.InnerText,
                        Position = node.SelectSingleNode("Position")?.InnerText
                    };

                    bool isMatch =
                        (criteria.FullName == null || teacher.FullName == criteria.FullName) &&
                        (criteria.Faculty == null || teacher.Faculty == criteria.Faculty) &&
                        (criteria.Department == null || teacher.Department == criteria.Department) &&
                        (criteria.Position == null || teacher.Position== criteria.Position);

                    if (isMatch)
                    {
                        var eduNodes = node.SelectNodes("Educations/Education");
                        foreach (XmlNode eduNode in eduNodes)
                        {
                            teacher.Educations.Add(new Education
                            {
                                Level = eduNode.Attributes["Level"]?.Value,
                                Institution = eduNode.SelectSingleNode("Institution")?.InnerText,
                                Period = eduNode.SelectSingleNode("Period")?.InnerText
                            });
                        }
                        results.Add(teacher);
                    }
                }
                return results;
            }
        }

        public class Linq : IStrategy
        {
            public List<Teacher> Search(Teacher criteria)
            {
                try
                {
                    XDocument doc = XDocument.Load(xmlFilePath);
                    var query = from t in doc.Descendants("Teacher")
                                where
                                    (criteria.FullName == null || (string)t.Attribute("FullName") == criteria.FullName) &&
                                    (criteria.Faculty == null || (string)t.Element("Faculty") == criteria.Faculty) &&
                                    (criteria.Department == null || (string)t.Element("Department") == criteria.Department) &&
                                    (criteria.Position == null || (string)t.Element("Position") == criteria.Position)
                                select new Teacher
                                {
                                    FullName = (string)t.Attribute("FullName"),
                                    Faculty = (string)t.Element("Faculty"),
                                    Department = (string)t.Element("Department"),
                                    Position = (string)t.Element("Position"),
                                    Educations = (from edu in t.Descendants("Education")
                                                  select new Education
                                                  {
                                                      Level = (string)edu.Attribute("Level"),
                                                      Institution = (string)edu.Element("Institution"),
                                                      Period = (string)edu.Element("Period")
                                                  }).ToList()
                                };
                    return query.ToList();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LINQ Error: {ex.Message}");
                    return null;
                }
            }
        }

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            #if DEBUG
            builder.Logging.AddDebug();
            #endif
            return builder.Build();
        }
    }
}