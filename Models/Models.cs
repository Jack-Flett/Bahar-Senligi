using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BaharSenligi.Models
{
    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
    }

    public class Question
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = "";
        public string Answer { get; set; } = "";
        public Guid CategoryId { get; set; }
        public int Points { get; set; } = 1;
    }

    public class AppData {
        public ObservableCollection<Category> Categories { get; set; } = new();
        public List<Question> Questions { get; set; } = new();
        public ObservableCollection<PastCompetition> History { get; set; } = new();
        public HashSet<Guid> AskedQuestionIds { get; set; } = new();
    }

    public class Contestant : INotifyPropertyChanged {
        public string Name { get; set; } = "";

        private int _score;
        public int Score {
            get => _score;
            set { _score = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Score))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class CompetitionQuestion
    {
        public Question Question { get; set; } = null!;
        public string? AwardedTo { get; set; } = null; // contestant name, null = unanswered
    }

    public class PastCompetition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Date { get; set; } = DateTime.Now;
        public string Winner { get; set; } = "";
        public List<ContestantResult> Results { get; set; } = new();
        public List<PastQuestion> Questions { get; set; } = new();
    }

    public class ContestantResult
    {
        public string Name { get; set; } = "";
        public int Score { get; set; } = 0;
    }

    public class PastQuestion
    {
        public string Text { get; set; } = "";
        public string Answer { get; set; } = "";
        public string Category { get; set; } = "";
        public int Points { get; set; } = 1;
        public string? AwardedTo { get; set; }
    }
}
