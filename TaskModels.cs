using System.Collections.Generic;

namespace PricisApp
{
    /// <summary>
    /// Represents a category for tasks.
    /// </summary>
    public class Category
    {
        public int Id { get; }
        public string Name { get; }
        public string Color { get; }

        public Category(int id, string name, string color)
        {
            Id = id;
            Name = name ?? string.Empty;
            Color = color ?? "#FFFFFF";
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// Represents a task item.
    /// </summary>
    public class TaskItem
    {
        public int Id { get; }
        public string Name { get; }
        public bool IsComplete { get; set; }
        public Category? Category { get; set; }
        public IReadOnlyList<string>? Tags { get; set; }

        public TaskItem(int id, string name, bool isComplete = false, Category? category = null, IReadOnlyList<string>? tags = null)
        {
            Id = id;
            Name = name ?? string.Empty;
            IsComplete = isComplete;
            Category = category;
            Tags = tags ?? new List<string>();
        }

        public override string ToString() => Name;
    }
} 