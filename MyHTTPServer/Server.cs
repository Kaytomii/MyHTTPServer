using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Text.Json;
using System.Linq;
using System.IO;

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

                    byte[] bytes = Encoding.UTF8.GetBytes(responseHtml);
                    res.ContentLength64 = bytes.Length;
                    res.ContentType = "text/html; charset=utf-8";
                    res.StatusCode = 200;
                    await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
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