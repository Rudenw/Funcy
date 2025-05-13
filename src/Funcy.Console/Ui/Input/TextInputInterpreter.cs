namespace Funcy.Console.Ui.Input;

public static class TextInputInterpreter
{
    public static char? Interpret(ConsoleKeyInfo keyInfo)
    {
        if (!char.IsControl(keyInfo.KeyChar))
            return keyInfo.KeyChar;
        
        if (IsLetterKey(keyInfo.Key))
        {
            var baseChar = keyInfo.KeyChar;

            var isShift = (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0;
            var isCaps = OperatingSystem.IsWindows() && System.Console.CapsLock;
            var isUpper = isCaps ^ isShift;

            return isUpper ? char.ToUpperInvariant(baseChar) : char.ToLowerInvariant(baseChar);
        }
        
        return null;
    }

    private static bool IsLetterKey(ConsoleKey key) =>
        key is >= ConsoleKey.A and <= ConsoleKey.Z;
}