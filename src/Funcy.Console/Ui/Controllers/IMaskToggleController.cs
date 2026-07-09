namespace Funcy.Console.Ui.Controllers;

// Implemented by a controller whose panel supports revealing/masking the selected row.
public interface IMaskToggleController
{
    void ToggleSelectedMask();
}

// Implemented by a controller whose panel supports copying the selected row's revealed value.
public interface IClipboardCopyController
{
    void CopySelectedValue();
}
