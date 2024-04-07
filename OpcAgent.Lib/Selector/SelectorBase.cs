namespace OpcAgent.Selector;

public abstract class SelectorBase : ISelector
{
    public abstract void PrintMenu();
    public int ReadInput()
    {
        var keyPressed = Console.ReadKey();
        var isParsed = int.TryParse(keyPressed.KeyChar.ToString(), out int result);
        return isParsed ? result : -1;
    }
}