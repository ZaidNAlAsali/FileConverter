# Preset reference

ZFileConverter presets control which selected files appear in an Explorer action, the conversion settings used, the output path, and what happens to the source after a successful conversion.

## Output file path templates

Leave the template empty to write the converted file next to the input with the new extension.

Templates may include the following tokens:

| Token | Meaning |
|---|---|
| `(path)` or `(p)` | Input file's parent directory, including the trailing separator |
| `(filename)` or `(f)` | Input filename without its extension |
| `(F)` | Uppercase input filename without its extension |
| `(outputext)` or `(o)` | Lowercase output extension |
| `(O)` | Uppercase output extension |
| `(inputext)` or `(i)` | Input extension as supplied by the file path |
| `(I)` | Uppercase input extension |
| `(p:documents)` or `(p:d)` | Current user's Documents folder |
| `(p:music)` or `(p:m)` | Current user's Music folder |
| `(p:videos)` or `(p:v)` | Current user's Videos folder |
| `(p:pictures)` or `(p:p)` | Current user's Pictures folder |
| `(d0)`, `(d1)`, ... | Parent directory names, counted upward from the input file's directory |
| `(D0)`, `(D1)`, ... | Uppercase versions of the parent directory tokens |
| `(n:i)` | Current output number for conversions that produce several files |
| `(n:c)` | Total output count for conversions that produce several files |
| `(d:<format>)` | Current date/time using a .NET date/time format, for example `(d:yyyy-MM-dd)` |

ZFileConverter appends the selected output extension automatically. Do not add a literal extension to the template unless you intentionally want it in the base filename.

### Examples

Given this input:

```text
C:\Media\Source\clip.mov
```

| Template | Example output for MP4 |
|---|---|
| Empty | `C:\Media\Source\clip.mp4` |
| `(path)Converted\(filename)` | `C:\Media\Source\Converted\clip.mp4` |
| `(p:videos)Converted\(filename)` | `C:\Users\<you>\Videos\Converted\clip.mp4` |
| `(path)(filename) - converted` | `C:\Media\Source\clip - converted.mp4` |

The preview shown in Settings uses the same path-generation code as a real conversion. Treat a preview error as a configuration error and correct it before saving.

## Post-conversion actions

The post-conversion action runs **only after ZFileConverter confirms that the conversion succeeded and the expected output exists**.

| Action | Behavior |
|---|---|
| **None** | Leaves the input file where it is. This is the safest default. |
| **Move to archive folder** | Creates the preset's configured archive folder next to the input when necessary, then moves the input into it. Name collisions receive a unique path rather than overwriting an existing file. |
| **Delete** | Permanently deletes the input file after successful conversion. Windows Recycle Bin is not used. |

> [!CAUTION]
> Use **Delete** only after testing the preset on disposable files. A successful conversion followed by deletion is intentional and cannot be undone by ZFileConverter.

## Explorer behavior

- A preset appears only when at least one selected file has a compatible input extension.
- A preset is enabled only when it is compatible with every distinct selected extension.
- Preset folders become nested Explorer submenus.
- Saving Settings writes the current preset library immediately. A fresh Explorer context menu then reloads the file when its timestamp changes.

For broader product and build documentation, return to the [ZFileConverter README](../README.md).
