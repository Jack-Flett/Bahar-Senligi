using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BaharSenligi.Models;
using BaharSenligi.Services;

namespace BaharSenligi.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DataService _data = new();
        private readonly QuestionSelector _selector = new();
        public AppData AppData { get; private set; }

        // ── Navigation ──────────────────────────────────────────────
        public enum Screen { Main, Competition, Results, History, HistoryDetail }
        private Screen _currentScreen = Screen.Main;
        public Screen CurrentScreen
        {
            get => _currentScreen;
            set { _currentScreen = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsMain)); OnPropertyChanged(nameof(IsCompetition)); OnPropertyChanged(nameof(IsResults)); OnPropertyChanged(nameof(IsHistory)); OnPropertyChanged(nameof(IsHistoryDetail)); }
        }
        public bool IsMain => CurrentScreen == Screen.Main;
        public bool IsCompetition => CurrentScreen == Screen.Competition;
        public bool IsResults => CurrentScreen == Screen.Results;
        public bool IsHistory => CurrentScreen == Screen.History;
        public bool IsHistoryDetail => CurrentScreen == Screen.HistoryDetail;

        // ── Left panel (categories + questions) ─────────────────────
        private Guid? _selectedCategoryId;
        public Guid? SelectedCategoryId
        {
            get => _selectedCategoryId;
            set { _selectedCategoryId = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredQuestions)); OnPropertyChanged(nameof(SelectedCategory)); }
        }
        public Category? SelectedCategory => AppData.Categories.FirstOrDefault(c => c.Id == _selectedCategoryId);
        public List<Question> FilteredQuestions => _selectedCategoryId == null
            ? new()
            : AppData.Questions.Where(q => q.CategoryId == _selectedCategoryId).ToList();

        // ── Add/Edit Question panel ──────────────────────────────────
        private bool _showAddQuestion;
        public bool ShowAddQuestion { get => _showAddQuestion; set { _showAddQuestion = value; OnPropertyChanged(); } }
        private string _newQuestionText = "";
        public string NewQuestionText { get => _newQuestionText; set { _newQuestionText = value; OnPropertyChanged(); } }
        private string _newQuestionAnswer = "";
        public string NewQuestionAnswer { get => _newQuestionAnswer; set { _newQuestionAnswer = value; OnPropertyChanged(); } }
        private int _newQuestionPoints = 1;
        public int NewQuestionPoints { get => _newQuestionPoints; set { _newQuestionPoints = value; OnPropertyChanged(); } }
        private Guid _newQuestionCategoryId;
        public Guid NewQuestionCategoryId { get => _newQuestionCategoryId; set { _newQuestionCategoryId = value; OnPropertyChanged(); } }

        // ── Add Category panel ───────────────────────────────────────
        private bool _showAddCategory;
        public bool ShowAddCategory { get => _showAddCategory; set { _showAddCategory = value; OnPropertyChanged(); } }
        private string _newCategoryName = "";
        public string NewCategoryName { get => _newCategoryName; set { _newCategoryName = value; OnPropertyChanged(); } }

        // ── Competition Setup ────────────────────────────────────────
        private bool _showCompetitionSetup;
        public bool ShowCompetitionSetup { get => _showCompetitionSetup; set { _showCompetitionSetup = value; OnPropertyChanged(); } }
        private int _setupQuestionCount = 10;
        public int SetupQuestionCount { get => _setupQuestionCount; set { _setupQuestionCount = value; OnPropertyChanged(); } }
        private int _setupContestantCount = 2;
        public int SetupContestantCount
        {
            get => _setupContestantCount;
            set
            {
                _setupContestantCount = Math.Clamp(value, 2, 4);
                while (SetupContestantNames.Count < _setupContestantCount) SetupContestantNames.Add(new ContestantNameEntry());
                while (SetupContestantNames.Count > _setupContestantCount) SetupContestantNames.RemoveAt(SetupContestantNames.Count - 1);
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ContestantNameEntry> SetupContestantNames { get; } = new()
            { new ContestantNameEntry(), new ContestantNameEntry() };
        public ObservableCollection<CategoryToggle> SetupCategories { get; } = new();

        // ── Competition State ────────────────────────────────────────
        private List<CompetitionQuestion> _competitionQuestions = new();
        private int _currentQuestionIndex = 0;
        public int CurrentQuestionIndex
        {
            get => _currentQuestionIndex;
            set { _currentQuestionIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentCompetitionQuestion)); OnPropertyChanged(nameof(CurrentQuestion)); OnPropertyChanged(nameof(QuestionProgress)); OnPropertyChanged(nameof(CanGoPrevious)); OnPropertyChanged(nameof(CanGoNext)); AnswerRevealed = false; }
        }
        public CompetitionQuestion? CurrentCompetitionQuestion => _competitionQuestions.Count > 0 ? _competitionQuestions[_currentQuestionIndex] : null;
        public Question? CurrentQuestion => CurrentCompetitionQuestion?.Question;
        public string QuestionProgress => _competitionQuestions.Count == 0 ? "" : $"{_currentQuestionIndex + 1} / {_competitionQuestions.Count}";
        public bool CanGoPrevious => _currentQuestionIndex > 0;
        public bool CanGoNext => _currentQuestionIndex < _competitionQuestions.Count - 1;

        private bool _answerRevealed;
        public bool AnswerRevealed { get => _answerRevealed; set { _answerRevealed = value; OnPropertyChanged(); } }

        public ObservableCollection<Contestant> Contestants { get; } = new();

        private string? _setupWarning;
        public string? SetupWarning { get => _setupWarning; set { _setupWarning = value; OnPropertyChanged(); } }

        // ── Tiebreaker ───────────────────────────────────────────────
        private bool _showTiebreaker;
        public bool ShowTiebreaker { get => _showTiebreaker; set { _showTiebreaker = value; OnPropertyChanged(); } }
        private CompetitionQuestion? _tiebreakerQuestion;
        public CompetitionQuestion? TiebreakerQuestion { get => _tiebreakerQuestion; set { _tiebreakerQuestion = value; OnPropertyChanged(); OnPropertyChanged(nameof(TiebreakerQuestionText)); } }
        public string TiebreakerQuestionText => _tiebreakerQuestion?.Question.Text ?? "";
        private bool _tiebreakerAnswerRevealed;
        public bool TiebreakerAnswerRevealed { get => _tiebreakerAnswerRevealed; set { _tiebreakerAnswerRevealed = value; OnPropertyChanged(); } }
        private List<Contestant> _tiedContestants = new();
        public List<Contestant> TiedContestants {
            get => _tiedContestants;
            private set { _tiedContestants = value; OnPropertyChanged(); }
        }

        // ── Results ──────────────────────────────────────────────────
        public List<Contestant> FinalResults { get; private set; } = new();
        public string WinnerName { get; private set; } = "";

        // ── History ──────────────────────────────────────────────────
        private PastCompetition? _selectedHistory;
        public PastCompetition? SelectedHistory { get => _selectedHistory; set { _selectedHistory = value; OnPropertyChanged(); } }

        public MainViewModel()
        {
            AppData = _data.Load();
            InitCommands();
        }

        private void InitCommands()
        {
            IncreaseContestantsCommand = new RelayCommand(() => SetupContestantCount++);
            DecreaseContestantsCommand = new RelayCommand(() => SetupContestantCount--);
            ToggleCategoryCommand = new RelayCommand<CategoryToggle>(t => { if (t != null) t.IsEnabled = !t.IsEnabled; });

            OpenAddQuestionCommand = new RelayCommand(() => { NewQuestionText = ""; NewQuestionAnswer = ""; NewQuestionPoints = 1; NewQuestionCategoryId = SelectedCategoryId ?? (AppData.Categories.FirstOrDefault()?.Id ?? Guid.Empty); ShowAddQuestion = true; });
            CloseAddQuestionCommand = new RelayCommand(() => ShowAddQuestion = false);
            SaveQuestionCommand = new RelayCommand(SaveQuestion);

            OpenAddCategoryCommand = new RelayCommand(() => { NewCategoryName = ""; ShowAddCategory = true; });
            CloseAddCategoryCommand = new RelayCommand(() => ShowAddCategory = false);
            SaveCategoryCommand = new RelayCommand(SaveCategory);
            DeleteCategoryCommand = new RelayCommand<Category>(DeleteCategory);
            DeleteQuestionCommand = new RelayCommand<Question>(DeleteQuestion);
            SelectCategoryCommand = new RelayCommand<Category>(c => SelectedCategoryId = c?.Id);

            OpenCompetitionSetupCommand = new RelayCommand(OpenCompetitionSetup);
            CloseCompetitionSetupCommand = new RelayCommand(() => ShowCompetitionSetup = false);
            StartCompetitionCommand = new RelayCommand(StartCompetition);

            RevealAnswerCommand = new RelayCommand(() => AnswerRevealed = true);
            AwardPointCommand = new RelayCommand<Contestant>(AwardPoint);
            RemovePointCommand = new RelayCommand(() => { if (CurrentCompetitionQuestion != null) { var prev = Contestants.FirstOrDefault(c => c.Name == CurrentCompetitionQuestion.AwardedTo); if (prev != null) prev.Score -= CurrentCompetitionQuestion.Question.Points; CurrentCompetitionQuestion.AwardedTo = null; OnPropertyChanged(nameof(CurrentCompetitionQuestion)); } });
            NextQuestionCommand = new RelayCommand(NextQuestion, () => CanGoNext);
            PreviousQuestionCommand = new RelayCommand(() => CurrentQuestionIndex--, () => CanGoPrevious);
            FinishCompetitionCommand = new RelayCommand(FinishCompetition);

            AwardTiebreakerCommand = new RelayCommand<Contestant>(AwardTiebreaker);
            RevealTiebreakerAnswerCommand = new RelayCommand(() => TiebreakerAnswerRevealed = true);

            GoToHistoryCommand = new RelayCommand(() => CurrentScreen = Screen.History);
            ViewHistoryDetailCommand = new RelayCommand<PastCompetition>(h => { SelectedHistory = h; CurrentScreen = Screen.HistoryDetail; });
            BackToHistoryCommand = new RelayCommand(() => CurrentScreen = Screen.History);
            BackToMainCommand = new RelayCommand(() => CurrentScreen = Screen.Main);
            ResetQuestionPoolCommand = new RelayCommand(() => { AppData.AskedQuestionIds.Clear(); _data.Save(AppData); });
        }

        // ── Commands ─────────────────────────────────────────────────
        public ICommand IncreaseContestantsCommand { get; private set; } = null!;
        public ICommand DecreaseContestantsCommand { get; private set; } = null!;
        public ICommand ToggleCategoryCommand { get; private set; } = null!;
        public ICommand OpenAddQuestionCommand { get; private set; } = null!;
        public ICommand CloseAddQuestionCommand { get; private set; } = null!;
        public ICommand SaveQuestionCommand { get; private set; } = null!;
        public ICommand OpenAddCategoryCommand { get; private set; } = null!;
        public ICommand CloseAddCategoryCommand { get; private set; } = null!;
        public ICommand SaveCategoryCommand { get; private set; } = null!;
        public ICommand DeleteCategoryCommand { get; private set; } = null!;
        public ICommand DeleteQuestionCommand { get; private set; } = null!;
        public ICommand SelectCategoryCommand { get; private set; } = null!;
        public ICommand OpenCompetitionSetupCommand { get; private set; } = null!;
        public ICommand CloseCompetitionSetupCommand { get; private set; } = null!;
        public ICommand StartCompetitionCommand { get; private set; } = null!;
        public ICommand RevealAnswerCommand { get; private set; } = null!;
        public ICommand AwardPointCommand { get; private set; } = null!;
        public ICommand RemovePointCommand { get; private set; } = null!;
        public ICommand NextQuestionCommand { get; private set; } = null!;
        public ICommand PreviousQuestionCommand { get; private set; } = null!;
        public ICommand FinishCompetitionCommand { get; private set; } = null!;
        public ICommand AwardTiebreakerCommand { get; private set; } = null!;
        public ICommand RevealTiebreakerAnswerCommand { get; private set; } = null!;
        public ICommand GoToHistoryCommand { get; private set; } = null!;
        public ICommand ViewHistoryDetailCommand { get; private set; } = null!;
        public ICommand BackToHistoryCommand { get; private set; } = null!;
        public ICommand BackToMainCommand { get; private set; } = null!;
        public ICommand ResetQuestionPoolCommand { get; private set; } = null!;

        // ── Logic ────────────────────────────────────────────────────
        private void SaveQuestion()
        {
            if (string.IsNullOrWhiteSpace(NewQuestionText) || string.IsNullOrWhiteSpace(NewQuestionAnswer)) return;
            AppData.Questions.Add(new Question
            {
                Text = NewQuestionText.Trim(),
                Answer = NewQuestionAnswer.Trim(),
                CategoryId = NewQuestionCategoryId,
                Points = NewQuestionPoints
            });
            _data.Save(AppData);
            ShowAddQuestion = false;
            OnPropertyChanged(nameof(FilteredQuestions));
        }

        private void SaveCategory() 
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName)) return;
            AppData.Categories.Add(new Category { Name = NewCategoryName.Trim() });
            _data.Save(AppData);
            ShowAddCategory = false;
        }

        private void DeleteCategory(Category? cat)
        {
            if (cat == null) return;
            var result = MessageBox.Show($"'{cat.Name}' kategorisi ve bu kategorideki tüm sorular silinecek. Emin misin?", "Kategori Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            AppData.Questions.RemoveAll(q => q.CategoryId == cat.Id);
            AppData.Categories.Remove(cat);
            if (SelectedCategoryId == cat.Id) SelectedCategoryId = null;
            _data.Save(AppData);
            OnPropertyChanged(nameof(FilteredQuestions));
        }

        private void DeleteQuestion(Question? q)
        {
            if (q == null) return;
            AppData.Questions.Remove(q);
            _data.Save(AppData);
            OnPropertyChanged(nameof(FilteredQuestions));
        }

        private void OpenCompetitionSetup()
        {
            SetupCategories.Clear();
            foreach (var cat in AppData.Categories)
                SetupCategories.Add(new CategoryToggle { Category = cat, IsEnabled = true });
            SetupContestantCount = 2;
            foreach (var n in SetupContestantNames) n.Name = "";
            SetupQuestionCount = 10;
            SetupWarning = null;
            ShowCompetitionSetup = true;
        }

        private void StartCompetition()
        {
            var names = SetupContestantNames.Take(SetupContestantCount).Select(n => n.Name.Trim()).ToList();
            if (names.Any(string.IsNullOrWhiteSpace))
            {
                SetupWarning = "Lütfen tüm yarışmacı isimlerini girin.";
                return;
            }

            var selectedCatIds = SetupCategories.Where(t => t.IsEnabled).Select(t => t.Category.Id).ToList();
            if (selectedCatIds.Count == 0)
            {
                SetupWarning = "En az bir kategori seçmelisin.";
                return;
            }

            var result = _selector.Select(AppData.Questions, selectedCatIds, SetupQuestionCount, AppData.AskedQuestionIds);
            SetupWarning = result.Warning;

            if (result.Questions.Count == 0)
            {
                SetupWarning = "Seçili kategorilerde hiç soru bulunamadı.";
                return;
            }

            // Track asked
            foreach (var q in result.Questions) AppData.AskedQuestionIds.Add(q.Id);

            _competitionQuestions = result.Questions.Select(q => new CompetitionQuestion { Question = q }).ToList();

            Contestants.Clear();
            foreach (var name in names) Contestants.Add(new Contestant { Name = name });

            CurrentQuestionIndex = 0;
            AnswerRevealed = false;
            ShowCompetitionSetup = false;
            CurrentScreen = Screen.Competition;
        }

        private void AwardPoint(Contestant? contestant)
        {
            if (contestant == null || CurrentCompetitionQuestion == null) return;
            // Remove previous award if any
            if (CurrentCompetitionQuestion.AwardedTo != null)
            {
                var prev = Contestants.FirstOrDefault(c => c.Name == CurrentCompetitionQuestion.AwardedTo);
                if (prev != null) prev.Score -= CurrentCompetitionQuestion.Question.Points;
            }
            CurrentCompetitionQuestion.AwardedTo = contestant.Name;
            contestant.Score += CurrentCompetitionQuestion.Question.Points;
            OnPropertyChanged(nameof(CurrentCompetitionQuestion));
        }

        private void NextQuestion()
        {
            if (CanGoNext) CurrentQuestionIndex++;
        }

        private void FinishCompetition()
        {
            var sorted = Contestants.OrderByDescending(c => c.Score).ToList();
            int topScore = sorted[0].Score;
            var tied = sorted.Where(c => c.Score == topScore).ToList();

            if (tied.Count > 1)
            {
                // Start tiebreaker
                TiedContestants = tied;
                var tbPool = AppData.Questions
                    .Where(q => SetupCategories.Any(t => t.IsEnabled && t.Category.Id == q.CategoryId))
                    .Where(q => !_competitionQuestions.Any(cq => cq.Question.Id == q.Id))
                    .OrderBy(_ => Guid.NewGuid())
                    .FirstOrDefault();

                if (tbPool == null)
                {
                    // No tiebreaker questions available, just show results
                    ShowResults(sorted);
                    return;
                }

                TiebreakerQuestion = new CompetitionQuestion { Question = tbPool };
                TiebreakerAnswerRevealed = false;
                ShowTiebreaker = true;
                return;
            }

            ShowResults(sorted);
        }

        private void AwardTiebreaker(Contestant? winner)
        {
            if (winner == null) return;
            winner.Score += TiebreakerQuestion!.Question.Points;
            ShowTiebreaker = false;
            ShowResults(Contestants.OrderByDescending(c => c.Score).ToList());
        }

        private void ShowResults(List<Contestant> sorted)
        {
            FinalResults = sorted;
            WinnerName = sorted[0].Name;

            // Save to history
            var past = new PastCompetition
            {
                Winner = WinnerName,
                Results = sorted.Select(c => new ContestantResult { Name = c.Name, Score = c.Score }).ToList(),
                Questions = _competitionQuestions.Select(cq =>
                {
                    var cat = AppData.Categories.FirstOrDefault(c => c.Id == cq.Question.CategoryId);
                    return new PastQuestion
                    {
                        Text = cq.Question.Text,
                        Answer = cq.Question.Answer,
                        Category = cat?.Name ?? "?",
                        Points = cq.Question.Points,
                        AwardedTo = cq.AwardedTo
                    };
                }).ToList()
            };
            AppData.History.Insert(0, past);
            _data.Save(AppData);

            OnPropertyChanged(nameof(FinalResults));
            OnPropertyChanged(nameof(WinnerName));
            CurrentScreen = Screen.Results;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ContestantNameEntry : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name { get => _name; set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null) { _execute = execute; _canExecute = canExecute; }
        public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
        public bool CanExecute(object? p) => _canExecute?.Invoke(p is T t ? t : default) ?? true;
        public void Execute(object? p) => _execute(p is T t ? t : default);
    }

    public class CategoryToggle : INotifyPropertyChanged
    {
        public Category Category { get; set; } = null!;
        private bool _isEnabled = true;
        public bool IsEnabled { get => _isEnabled; set { _isEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
