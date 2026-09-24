using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

class P
{
    static void Main()
    {
        var path = "./src/EventApi/bin/Debug/net8.0/EventApi.dll";
        var asm = Assembly.LoadFrom(path);
        Console.WriteLine("Loaded " + asm.FullName);
        foreach (var t in asm.GetTypes())
        {
            if (typeof(Migration).IsAssignableFrom(t))
            {
                Console.WriteLine("Migration: " + t.FullName);
            }
        }
    }
}
