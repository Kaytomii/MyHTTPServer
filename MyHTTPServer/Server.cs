using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Text.Json;
using System.Linq;
using System.IO;
using System.Web;

namespace MyHTTPServer;

class User
{
    public string Login { get; set; } = null!;
    public string Pwd { get; set; } = null!;
}

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;

    public Student() { }
    public Student(int id, string name, string surname, string group)
    {
        Id = id;
        Name = name;
        Surname = surname;
        Group = group;
    }
}

internal class Server
{
    List<Student> students = new List<Student>
    {
        new Student(1, "Alex", "Brown", "CS-01"),
        new Student(2, "Maria", "Stone", "CS-01"),
        new Student(3, "John", "Miller", "CS-02"),
        new Student(4, "Sofia", "White", "CS-02"),
        new Student(5, "Mark", "Black", "CS-03"),
        new Student(6, "Anna", "Green", "CS-03"),
        new Student(7, "David", "King", "CS-01"),
        new Student(8, "Helen", "Moore", "CS-02"),
        new Student(9, "Tom", "Fox", "CS-03"),
        new Student(10, "Kate", "Hill", "CS-01")
    };

    readonly string _HOST = "http://127.0.0.1:8080/";

    public async Task RunServer()
    {
        HttpListener server = new HttpListener();
        server.Prefixes.Add(_HOST);
        server.Start();
        Console.WriteLine($"Server has been started on host: {_HOST}");

        while (true)
        {
            try
            {
                HttpListenerContext ctx = await server.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse res = ctx.Response;

                if (req.HttpMethod == "GET")
                {
                    string param = req.Url?.AbsolutePath ?? "/";
                    string responseHtml = "";

                    if (req.Url.AbsolutePath.StartsWith("/styles/"))
                    {
                        string fileName = req.Url.AbsolutePath.Replace("/styles/", "");
                        string fullPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "styles", fileName);

                        if (File.Exists(fullPath))
                        {
                            byte[] cssBytes = await File.ReadAllBytesAsync(fullPath);

                            res.ContentType = "text/css";
                            res.ContentLength64 = cssBytes.Length;

                            await res.OutputStream.WriteAsync(cssBytes, 0, cssBytes.Length);
                            res.Close();
                            continue;
                        }
                        else
                        {
                            res.StatusCode = 404;
                            res.Close();
                            continue;
                        }
                    }

                    if (param.StartsWith("/student"))
                    {
                        var parts = param.Split('/', StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length > 1)
                        {
                            string idStr = parts[1];
                            var stud = students.FirstOrDefault(s => s.Id.ToString() == idStr);
                            responseHtml = stud != null
                                ? $"<p>ID: {stud.Id}, Name: {stud.Name} {stud.Surname}, Group: {stud.Group}</p>"
                                : "<h1>Student not found</h1>";
                        }
                        else
                        {
                            var query = req.QueryString;
                            string? filterName = query["Name"];
                            string? filterGroup = query["Group"];

                            var filtered = students.AsEnumerable();

                            if (!string.IsNullOrEmpty(filterName))
                                filtered = filtered.Where(s => s.Name.Contains(filterName, StringComparison.OrdinalIgnoreCase));

                            if (!string.IsNullOrEmpty(filterGroup))
                                filtered = filtered.Where(s => s.Group.Contains(filterGroup, StringComparison.OrdinalIgnoreCase));

                            var resultList = filtered.ToList();
                            if (resultList.Any())
                            {
                                responseHtml = "<ul>" + string.Join("", resultList.Select(s => $"<li>{s.Id}: {s.Name} {s.Surname} - {s.Group}</li>")) + "</ul>";
                            }
                            else
                            {
                                responseHtml = "<p>No students found</p>";
                            }
                        }

                        string layoutPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "pages", "student.html");
                        string layout = await File.ReadAllTextAsync(layoutPath);
                        responseHtml = layout.Replace("{{content}}", responseHtml);
                    }

                    else
                    {
                        string page = GetPageName(param);
                        string path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "pages", page);
                        responseHtml = await File.ReadAllTextAsync(path);
                    }

                    if (req.Url.AbsolutePath.StartsWith("/images/"))
                    {
                        string fileName = req.Url.AbsolutePath.Replace("/images/", "");
                        string fullPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", fileName);

                        if (File.Exists(fullPath))
                        {
                            byte[] imgBytes = await File.ReadAllBytesAsync(fullPath);

                            res.ContentType = "image/" + Path.GetExtension(fullPath).Replace(".", "");
                            res.ContentLength64 = imgBytes.Length;

                            await res.OutputStream.WriteAsync(imgBytes, 0, imgBytes.Length);
                            res.Close();
                            continue;
                        }
                        else
                        {
                            res.StatusCode = 404;
                            res.Close();
                            continue;
                        }
                    }

                    byte[] bytes = Encoding.UTF8.GetBytes(responseHtml);
                    res.ContentLength64 = bytes.Length;
                    res.ContentType = "text/html; charset=utf-8";
                    res.StatusCode = 200;
                    await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                }
                //else if (req.HttpMethod == "POST")
                //{
                //    string body = "";
                //    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                //    {
                //        body = await reader.ReadToEndAsync();
                //        try
                //        {
                //            var formData = HttpUtility.ParseQueryString(body, Encoding.UTF8);

                //            string login = formData["login"];
                //            string password = formData["password"];
                //            string repeatPassword = formData["repeat_password"];
                //            bool isAgreed = formData["agree"] != null;

                //            List<string> errors = new List<string>();

                //            if (string.IsNullOrWhiteSpace(login) || login.Length <= 5)
                //            {
                //                errors.Add("Login must be more than 5 characters long.");
                //            }

                //            if (password != repeatPassword)
                //            {
                //                errors.Add("Passwords do not match.");
                //            }

                //            if (!isAgreed)
                //            {
                //                errors.Add("You must agree to the registration.");
                //            }

                //            object responseData;

                //            if (errors.Count == 0)
                //            {
                //                responseData = new
                //                {
                //                    name = login,
                //                    message = "Registration successful!"
                //                };
                //            }
                //            else
                //            {
                //                responseData = new
                //                {
                //                    errors = errors
                //                };
                //            }

                //            string jsonResponse = JsonSerializer.Serialize(responseData);
                //            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);

                //            res.ContentType = "application/json; charset=utf-8";
                //            res.ContentLength64 = buffer.Length;

                //            using (Stream output = res.OutputStream)
                //            {
                //                output.Write(buffer, 0, buffer.Length);
                //            }
                //        }
                //        catch (Exception ex)
                //        {

                //            Console.WriteLine(ex.Message);
                //        }
                //    }

                //}
                else if (req.HttpMethod == "POST")
                {
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        string body = await reader.ReadToEndAsync();
                        try
                        {
                            if (req.Url.AbsolutePath == "/student")
                            {
                                var newStudent = JsonSerializer.Deserialize<Student>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (newStudent != null)
                                {
                                    newStudent.Id = students.Max(s => s.Id) + 1;
                                    students.Add(newStudent);

                                    res.StatusCode = 201;
                                    byte[] buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newStudent));
                                    res.ContentType = "application/json; charset=utf-8";
                                    await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                                }
                            }
                            else
                            {
                                var formData = HttpUtility.ParseQueryString(body, Encoding.UTF8);
                                string login = formData["login"];
                                string password = formData["password"];
                                string repeatPassword = formData["repeat_password"];
                                bool isAgreed = formData["agree"] != null;

                                List<string> errors = new List<string>();
                                if (string.IsNullOrWhiteSpace(login) || login.Length <= 5) errors.Add("Login too short.");
                                if (password != repeatPassword) errors.Add("Passwords do not match.");
                                if (!isAgreed) errors.Add("Consent required.");

                                res.StatusCode = errors.Count == 0 ? 200 : 400;
                                var responseData = errors.Count == 0
                                    ? (object)new { name = login, message = "Registration successful!" }
                                    : new { errors };

                                string jsonResponse = JsonSerializer.Serialize(responseData);
                                byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
                                res.ContentType = "application/json; charset=utf-8";
                                await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            res.StatusCode = 500;
                        }
                    }
                }
                else if (req.HttpMethod == "PUT")
                {
                    if (req.Url.AbsolutePath.StartsWith("/student/"))
                    {
                        string idStr = req.Url.AbsolutePath.Replace("/student/", "");
                        if (int.TryParse(idStr, out int id))
                        {
                            var student = students.FirstOrDefault(s => s.Id == id);
                            if (student != null)
                            {
                                using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                                {
                                    string body = await reader.ReadToEndAsync();
                                    var updated = JsonSerializer.Deserialize<Student>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                    if (updated != null)
                                    {
                                        student.Name = updated.Name;
                                        student.Surname = updated.Surname;
                                        student.Group = updated.Group;

                                        res.StatusCode = 200;
                                        byte[] buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(student));
                                        res.ContentType = "application/json; charset=utf-8";
                                        await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                                    }
                                }
                            }
                            else
                            {
                                res.StatusCode = 404;
                            }
                        }
                    }
                }
                res.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    private string GetPageName(string param)
    {
        return param switch
        {
            "/contacts" => "contacts.html",
            "/about" => "about.html",
            "/" => "index.html",
            _ => "notfound.html"
        };
    }
}