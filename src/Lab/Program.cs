// See https://aka.ms/new-console-template for more information


using System.Text.RegularExpressions;

Type type = typeof((User, User?));

Console.WriteLine("Hello, {type}");

MyRegex().Matches("Hello, World!");

class User;

partial class Program
{
    [GeneratedRegex("Hello, World!")]
    private static partial Regex MyRegex();
}
