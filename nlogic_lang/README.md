# Visual Studio Code Extension for Syntax Highlighting

## VSIX (Using the Extension)

The `.vsix` file is a Visual Studio Code extension that enables syntax highlighting of Nlogic assembly files (`.pro`, `.pro.debug`, `.nlog`, and `.nlogic`).
The extension also includes a proper VS Code theme called "Nlogic Syntax," but it is incomplete. It is easier to customize the colors in VS Code's `settings.json`.

### Extension Installation
Open a folder cotaining the `.vsix` file in Visual Studio Code; right click the `.vsix` file, and click install.

### Configuring Syntax Highlighting

Once the extension is installed, tokens in Nlogic assembly will be annotated with syntax types (TextMate scopes), but will probably not be colored.
You can confirm that the extension is working correctly by opening an Nlogic assembly file and selecting "Developer: Inspect Editor Tokens and Scopes" from the command pallette (`ctrl+shift+p`).
Highlight tokens in the assembly file and confirm that the "textmate scopes" field has an `nlogic` suffix or prefix.

In order to get colorful syntax highlighting, style rules targeting these scopes need to be added to Visual Studio Code. The easiest way to do this is in `settings.json`.

## Styling Nlogic Tokens
Add `editor.tokenColorCustomizations` as a top-level key in the VS Code `settings.json`; the value should be an object with a `textMateRules` key, which has an array value. This array will contain an object for each TextMate scope style rule.

Inside `settings.json`:
```
{
    "editor.tokenColorCustomizations": {
        "textMateRuels": [
            {
                "scope": "nlogic.fill",
                "settings": {
                    "foreground": "#0000FF"
                }
            }
        ]
    }
}
```

Add any scope (which can be seen with the scope inspector described above) to this array to customize the highlighting.

A full set of scope selectors and style rules is in `nlogic_highlighting_customization.json`; copy the contents of the object in that file directly into the `settings.json` file as described above.

<br/><br/><hr/>

## nlogic-lang (Modifying the Extension)

The `nlogic-lang` folder contains the source for building the Nlogic syntax extension. It can be built and packaged into a `.vsix` file that can be installed as a VS Code extension by using the `vsce package` command. A guide for doing this is here: https://code.visualstudio.com/api/working-with-extensions/publishing-extension#usage .

### Modiyfing the Language Grammar

The rules for highlighting the syntax are specified with a TextMate grammar. The grammar definition is in `nlogic-lang/syntaxes/nlogic.tmLanguage.json`. To make a change to the way scopes are assigned to Nlogic tokens, modify that file, re-package the extension with `vsce package`, and install the newly created `.vsix` file.

If updating the extension does not seem to be working after installing the new `.vsix`, try:

1. Uninstall the existing `Nlogic Lang` extension from Visual Studio Code.

2. Delete the existing `.vsix` package from `nlogic-lang` (where the output of `vsce package` is placed).

3. Increase the version number in `nlogic-lang/package.json`.

4. Re-package and install the extension.

### TextMate Grammar

VS Code uses TextMate grammars. These are not specific to VS Code, and there are resources online for understanding how they work.
https://macromates.com/manual/en/language_grammars