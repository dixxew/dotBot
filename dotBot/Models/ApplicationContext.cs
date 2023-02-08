using dotBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
public class ApplicationContext : DbContext
{
    public ApplicationContext()
    {
    }

    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
    {
        //Database.EnsureCreated();   // создаем базу данных при первом обращении
    }

    public DbSet<User> Users { get; set; }
    public DbSet<GameStat> GameStats { get; set; }
}

