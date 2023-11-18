namespace ChickenBotV4
{
    internal static class ChickenBot
    {
        static void Main()
        {
            // Initialise our variables
            var chickenFeet = ChickenMarks.CreateLogger();
            
            chickenFeet.Debug("Debug");
            chickenFeet.Error("Err");
            chickenFeet.Warning("Warn");
            chickenFeet.Fatal("Fatal");
            
            // Print success message
            chickenFeet.Information("Chicken Bot V4 Is Online!");
        }
    }
}
