# Features

One folder per editor feature. Every folder has the same shape:

```
Features/<Name>/
├─ <Name>Feature.cs     # implements IEditorFeature
├─ <Name>Commands.cs    # command ids (constants)
├─ <Name>Model.cs       # optional: pure, testable logic
└─ README.md            # role, Monaco source, known limits
```

Enabled features are listed in `EditorFeatures.cs` — and nowhere else. See docs/how-to/add-a-feature.md.
