namespace SpecialRoles.Core;

/// <summary>
/// Texte für die Kill-Knopf-Schrift (Knöpfe und Hinweise): sie hat kein „ß“ – aus „Schießen“ wurde im Spiel „SCHIESEN“.
/// Umlaute hat sie.
/// </summary>
public static class ButtonText
{
    public static string Safe(string text) => text.Replace("ß", "ss");
}
