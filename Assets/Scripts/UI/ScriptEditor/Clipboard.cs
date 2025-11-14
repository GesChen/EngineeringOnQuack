using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using cfg = Config.ScriptEditor;

public class Clipboard {
    public ScriptEditorRewritten SE;

    // 0 is oldest ^1 is newest
    public List<string> Memory = new();

    public void Copy() {
        string singleSelection = SE.Carets.Carets[0].GetSelection();

        Memory.Add(singleSelection);

        if (Memory.Count > cfg.MaxClipboardSize)
            Memory.RemoveAt(0);
    }

    public void Paste() {
        string entry = Memory[^1];

        SE.Carets.Type(entry);
    }

    public void Cut() {
        Copy();
        SE.Carets.Type(""); // plz work
    }
}