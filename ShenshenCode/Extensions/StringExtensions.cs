namespace Shenshen.ShenshenCode.Extensions;

public static class StringExtensions
{
    // Godot resource paths use forward slashes on both macOS and Windows.
    public static string CharacterUiPath(this string filename)
    {
        return $"{MainFile.ResPath}/images/charui/{filename}";
    }
}
