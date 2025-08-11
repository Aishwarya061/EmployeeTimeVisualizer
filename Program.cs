using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing;

class EmployeeTime
{
    public string EmployeeName { get; set; }
    public double TotalHours { get; set; }
}

class TimeEntry
{
    public string EmployeeName { get; set; }
    public DateTime StarTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
}

class Program
{
    static async Task Main()
    {
        string url = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";

        using HttpClient client = new HttpClient();
        var json = await client.GetStringAsync(url);

        var entries = JsonSerializer.Deserialize<List<TimeEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var employeeHours = entries
            .GroupBy(e => e.EmployeeName)
            .Select(g => new EmployeeTime
            {
                EmployeeName = g.Key,
                TotalHours = g.Sum(e => (e.EndTimeUtc - e.StarTimeUtc).TotalHours)
            })
            .OrderByDescending(e => e.TotalHours)
            .ToList();

        // Generate HTML table
        string html = "<html><body><h2>Employee Hours</h2><table border='1' cellpadding='5'><tr><th>Name</th><th>Total Hours</th></tr>";
        foreach (var emp in employeeHours)
        {
            string rowColor = emp.TotalHours < 100 ? " style='background-color: #ffcccc;'" : "";
            html += $"<tr{rowColor}><td>{emp.EmployeeName}</td><td>{emp.TotalHours:F2}</td></tr>";
        }
        html += "</table></body></html>";

        File.WriteAllText("employees.html", html);
        Console.WriteLine("✅ HTML table created: employees.html");

        // Generate Pie Chart PNG
        int width = 600, height = 400;
        using Bitmap bmp = new Bitmap(width, height);
        using Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.White);

        double totalHours = employeeHours.Sum(e => e.TotalHours);
        float startAngle = 0f;
        Random rand = new Random();

        foreach (var emp in employeeHours)
        {
            float sweepAngle = (float)(emp.TotalHours / totalHours * 360);
            Color color = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
            using Brush brush = new SolidBrush(color);
            g.FillPie(brush, 50, 50, 300, 300, startAngle, sweepAngle);
            g.DrawString($"{emp.EmployeeName} ({emp.TotalHours:F1}h)", new Font("Arial", 10),
                Brushes.Black, 370, 50 + (15 * employeeHours.IndexOf(emp)));
            startAngle += sweepAngle;
        }

        bmp.Save("piechart.png");
        Console.WriteLine("✅ Pie chart created: piechart.png");
    }
}
