using System;
using System.Collections.Generic;

namespace Lab_oop_2
{
 
    // 1. Ієрархія ігор
    
    public abstract class BaseGame
    {
        private static int s_globalGameId = 1;

        public int GameId { get; }
        public string OpponentName { get; }
        public string GameTypeName { get; }

        protected BaseGame(string opponentName, string gameTypeName)
        {
            if (string.IsNullOrWhiteSpace(opponentName))
            {
                throw new ArgumentException("Ім'я суперника не може бути порожнім.", nameof(opponentName));
            }

            GameId = s_globalGameId++;
            OpponentName = opponentName;
            GameTypeName = gameTypeName;
        }

        // Абстрактний метод визначення рейтингу, на який відбувається гра
        public abstract int GetRatingForGame(int currentRating);
    }

    // Стандартна гра на фіксований рейтинг
    public class StandardGame : BaseGame
    {
        private readonly int _baseRating;

        public StandardGame(string opponentName, int baseRating)
            : base(opponentName, "Стандартна")
        {
            if (baseRating < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRating), "Рейтинг гри не може бути від'ємним.");
            }

            _baseRating = baseRating;
        }

        public override int GetRatingForGame(int currentRating)
        {
            return _baseRating;
        }
    }

    // Тренувальна гра — без рейтингу (0 балів)
    public class TrainingGame : BaseGame
    {
        public TrainingGame(string opponentName)
            : base(opponentName, "Тренувальна")
        {
        }

        public override int GetRatingForGame(int currentRating)
        {
            return 0;
        }
    }

    // Гра, де на рейтинг грає лише один гравець (половина базового рейтингу)
    public class SingleRatedGame : BaseGame
    {
        private readonly int _baseRating;

        public SingleRatedGame(string opponentName, int baseRating)
            : base(opponentName, "Одноосібний рейтинг")
        {
            if (baseRating < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRating), "Рейтинг гри не може бути від'ємним.");
            }

            _baseRating = baseRating;
        }

        public override int GetRatingForGame(int currentRating)
        {
            return Math.Max(1, _baseRating / 2);
        }
    }

 
    // 2. Фабрика ігор
    
    public static class GameFactory
    {
        public static BaseGame CreateStandardGame(string opponentName, int rating)
        {
            StandardGame game = new StandardGame(opponentName, rating);
            return (BaseGame)game; 
        }

        public static BaseGame CreateTrainingGame(string opponentName)
        {
            TrainingGame game = new TrainingGame(opponentName);
            return (BaseGame)game;
        }

        public static BaseGame CreateSingleRatedGame(string opponentName, int rating)
        {
            SingleRatedGame game = new SingleRatedGame(opponentName, rating);
            return (BaseGame)game;
        }
    }

    // Запис у журналі історії гравця
    public class GameRecord
    {
        public int GameId { get; }
        public string OpponentName { get; }
        public string GameTypeName { get; }
        public bool IsWin { get; }
        public int RatingChange { get; }

        public GameRecord(int gameId, string opponentName, string gameTypeName, bool isWin, int ratingChange)
        {
            GameId = gameId;
            OpponentName = opponentName;
            GameTypeName = gameTypeName;
            IsWin = isWin;
            RatingChange = ratingChange;
        }
    }


    // 3. Ієрархія акаунтів гравців

    public abstract class GameAccount
    {
        private readonly List<GameRecord> _gameHistory = new List<GameRecord>();

        public string UserName { get; }
        public int CurrentRating { get; protected set; }
        public int WinStreak { get; protected set; }
        public int GamesCount => _gameHistory.Count;
        public abstract string AccountTypeName { get; }

        protected GameAccount(string userName, int initialRating = 100)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Ім'я гравця не може бути порожнім.", nameof(userName));
            }

            if (initialRating < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(initialRating), "Початковий рейтинг не може бути менше 1.");
            }

            UserName = userName;
            CurrentRating = initialRating;
            WinStreak = 0;
        }

        // Віртуальні методи розрахунку зміни балів 
        protected virtual int CalculateWinPoints(BaseGame game)
        {
            return game.GetRatingForGame(CurrentRating);
        }

        protected virtual int CalculateLosePoints(BaseGame game)
        {
            return game.GetRatingForGame(CurrentRating);
        }

        public void WinGame(BaseGame game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game), "Об'єкт гри не може бути null.");
            }

            BaseGame baseGame = game; 
            int points = CalculateWinPoints(baseGame);
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), "Нараховані бали не можуть бути від'ємними.");
            }

            CurrentRating += points;
            WinStreak++;

            _gameHistory.Add(new GameRecord(
                baseGame.GameId,
                baseGame.OpponentName,
                baseGame.GameTypeName,
                isWin: true,
                ratingChange: points
            ));
        }

        public void LoseGame(BaseGame game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game), "Об'єкт гри не може бути null.");
            }

            BaseGame baseGame = game;
            int penalty = CalculateLosePoints(baseGame);
            if (penalty < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(penalty), "Штрафні бали не можуть бути від'ємними.");
            }

            CurrentRating -= penalty;
            if (CurrentRating < 1)
            {
                CurrentRating = 1;
            }

            WinStreak = 0;

            _gameHistory.Add(new GameRecord(
                baseGame.GameId,
                baseGame.OpponentName,
                baseGame.GameTypeName,
                isWin: false,
                ratingChange: penalty
            ));
        }

        // Збереження попереднього виклику за рейтингом як сумісності
        public void WinGame(string opponentName, int rating)
        {
            WinGame(GameFactory.CreateStandardGame(opponentName, rating));
        }

        public void LoseGame(string opponentName, int rating)
        {
            LoseGame(GameFactory.CreateStandardGame(opponentName, rating));
        }

        public void GetStats()
        {
            Console.WriteLine($"Статистика гравця {UserName} [{AccountTypeName}]:");
            Console.WriteLine($"Поточний рейтинг: {CurrentRating} | Серія перемог: {WinStreak} | Всього ігор: {GamesCount}");
            Console.WriteLine("Історія ігор:");

            if (_gameHistory.Count == 0)
            {
                Console.WriteLine("  Ігор ще не було.");
            }
            else
            {
                foreach (var record in _gameHistory)
                {
                    string res = record.IsWin ? "перемога" : "поразка";
                    string sign = record.IsWin ? "+" : "-";
                    Console.WriteLine($"  Гра #{record.GameId} [{record.GameTypeName}] проти {record.OpponentName}: {res} ({sign}{record.RatingChange} балів)");
                }
            }

            Console.WriteLine();
        }
    }

    // 1. Стандартний акаунт
    public class StandardGameAccount : GameAccount
    {
        public override string AccountTypeName => "Стандартний";

        public StandardGameAccount(string userName, int initialRating = 100)
            : base(userName, initialRating)
        {
        }
    }

    // 2. Акаунт, у якого при програші знімається вдвічі менше балів
    public class ReducedPenaltyGameAccount : GameAccount
    {
        public override string AccountTypeName => "Знижений штраф";

        public ReducedPenaltyGameAccount(string userName, int initialRating = 100)
            : base(userName, initialRating)
        {
        }

        protected override int CalculateLosePoints(BaseGame game)
        {
            int basePenalty = base.CalculateLosePoints(game);
            return basePenalty / 2;
        }
    }

    // 3. Акаунт із додатковими балами за серію перемог
    public class StreakBonusGameAccount : GameAccount
    {
        public override string AccountTypeName => "Бонус за серію";

        public StreakBonusGameAccount(string userName, int initialRating = 100)
            : base(userName, initialRating)
        {
        }

        protected override int CalculateWinPoints(BaseGame game)
        {
            int basePoints = base.CalculateWinPoints(game);
            if (basePoints == 0)
            {
                return 0; 
            }

            // За кожну перемогу в поточній серії додаємо +5 балів
            int bonus = (WinStreak + 1) * 5;
            return basePoints + bonus;
        }
    }

  
    // 4.Демонстрація програми
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            GameAccount acc1 = new StandardGameAccount("Олександр", 120);
            GameAccount acc2 = new ReducedPenaltyGameAccount("Марія", 120);
            GameAccount acc3 = new StreakBonusGameAccount("Дмитро", 100);

            // Гра 1: Стандартна гра (20 балів)
            BaseGame g1 = GameFactory.CreateStandardGame("Марія", 20);
            acc1.WinGame(g1);
            acc2.LoseGame(g1);

            // Гра 2: Стандартна гра (30 балів) — перемагає Дмитро
            BaseGame g2 = GameFactory.CreateStandardGame("Олександр", 30);
            acc3.WinGame(g2);
            acc1.LoseGame(g2);

            // Гра 3: Ще одна перемога Дмитра (серія зростає!)
            BaseGame g3 = GameFactory.CreateStandardGame("Марія", 25);
            acc3.WinGame(g3);
            acc2.LoseGame(g3);

            // Гра 4: Тренувальна гра (0 балів)
            BaseGame g4 = GameFactory.CreateTrainingGame("Дмитро");
            acc1.WinGame(g4);
            acc3.LoseGame(g4);

            // Гра 5: Гра з одноосібним рейтингом
            BaseGame g5 = GameFactory.CreateSingleRatedGame("Олександр", 40);
            acc2.WinGame(g5);

            // Сумісність зі старим викликом (через рейтинг напряму)
            acc1.WinGame("Дмитро", 15);

            acc1.GetStats();
            acc2.GetStats();
            acc3.GetStats();

            // Перевірка валідації при передачі null
            try
            {
                acc1.WinGame((BaseGame)null!);
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"Перевірка помилки (null): {ex.Message}");
            }
        }
    }
}