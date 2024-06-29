using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class TestCreator : MonoBehaviour
{
    public int a;
    [ContextMenu("UndoMerge")]
    public void Move() {
        Undo.CollapseUndoOperations(a);
    }
}
