using MaterialEditorAPI;

internal static class UiRowActionsPhaseSixContractTests
{
    internal static void Run()
    {
        LeaseReplacesOwnersAndClearsBeforeInvocation();
        LeaseRejectsStaleDisabledAndExcessActions();
        OneCanvasSurfaceOwnsFourPermanentActions();
        RendererAndMaterialRowsExposeOnlyTheApprovedActions();
        RowBindingInvalidatesEveryRetainedActionContext();
        ExistingDirectControlsAndObjDeferralRemainIntact();
        Console.WriteLine("Phase 6 row-action regression guards passed.");
    }

    private static void LeaseReplacesOwnersAndClearsBeforeInvocation()
    {
        var lease = new RowActionMenuLease();
        var ownerA = new object();
        var ownerB = new object();
        var invokedA = 0;
        var invokedB = 0;

        lease.Begin(ownerA, 1);
        Equal(0, lease.Add(() => invokedA++), "owner A first action");
        lease.Begin(ownerB, 2);
        Equal(0, lease.Add(() =>
        {
            Equal(false, lease.IsOpen, "lease closed inside callback");
            Equal(null, lease.Owner, "owner cleared inside callback");
            Equal(0, lease.Count, "callbacks cleared inside callback");
            invokedB++;
        }), "owner B replaces owner A");

        var callback = lease.Take(0, ownerB, 2, true, true);
        Equal(false, lease.IsOpen, "take closes before returning callback");
        callback();
        Equal(0, invokedA, "replaced owner cannot execute");
        Equal(1, invokedB, "current owner executes exactly once");
        Equal(null,
            lease.Take(0, ownerB, 2, true, true),
            "taken callback cannot execute twice");
    }

    private static void LeaseRejectsStaleDisabledAndExcessActions()
    {
        var owner = new object();
        var lease = new RowActionMenuLease();
        lease.Begin(owner, 10);
        Equal(-1, lease.Add(null), "null optional action omitted");
        for (var index = 0; index < RowActionMenuLease.MaximumActions; index++)
            Equal(index, lease.Add(() => { }), "bounded action slot " + index);
        Equal(-1, lease.Add(() => { }), "fifth action rejected");
        Equal(null,
            lease.Take(0, owner, 9, true, true),
            "stale generation rejected");
        Equal(false, lease.IsOpen, "stale take clears lease");

        lease.Begin(owner, 11);
        lease.Add(() => { });
        Equal(null,
            lease.Take(0, owner, 11, false, true),
            "inactive owner rejected");

        lease.Begin(owner, 12);
        lease.Add(() => { });
        Equal(null,
            lease.Take(0, owner, 12, true, false),
            "disabled action rejected");

        lease.Begin(owner, 13);
        lease.Add(() => { });
        lease.CloseIfOwner(new object());
        Equal(true, lease.IsOpen, "unrelated owner cannot close lease");
        lease.CloseIfOwner(owner);
        Equal(false, lease.IsOpen, "owning row closes lease");
    }

    private static void OneCanvasSurfaceOwnsFourPermanentActions()
    {
        var menu = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowActionMenu.cs");
        var window = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.WindowView.cs");
        var popup = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.PopupMenu.cs");
        var topBar = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.TopBarView.cs");

        Equal(1,
            CountOccurrences(window, ".AddComponent<MaterialEditorRowActionMenu>()"),
            "one menu component on the canvas");
        Equal(true,
            window.IndexOf("_rowActionMenu.Initialize(", StringComparison.Ordinal)
            < window.IndexOf("RowViewFactory.CreateTemplate(", StringComparison.Ordinal),
            "canvas-level menu initializes before pooled rows");
        Contains(window,
            "_rowActionMenu.Close();",
            "presentation release closes the shared menu");
        Contains(window,
            "_topBar.CloseGlobalMenu);",
            "row actions close the global popup before opening");
        Contains(window,
            "_rowActionMenu.Close);",
            "global popup closes row actions before toggling");
        Contains(topBar,
            "_beforeGlobalMenuOpen?.Invoke();",
            "global-popup mutual exclusion hook");
        Contains(menu,
            "\"MaterialEditorRowActionMenuDismissLayer\"",
            "full-canvas dismiss layer");
        Contains(menu,
            "new Button[RowActionMenuLease.MaximumActions]",
            "fixed four-button storage");
        Equal(4,
            CountOccurrences(menu, ".onClick.AddListener(Invoke"),
            "four permanent action listeners");

        var openRenderer = Slice(
            menu,
            "internal void OpenRenderer(",
            "internal void OpenMaterial(");
        var openMaterial = Slice(
            menu,
            "internal void OpenMaterial(",
            "internal void CloseIfOwner(");
        foreach (var opening in new[] { openRenderer, openMaterial })
        foreach (var forbidden in new[]
                 {
                     "new GameObject",
                     "Instantiate(",
                     "AddListener(",
                     "List<",
                     "Enumerable",
                     "GetMethod("
                 })
            DoesNotContain(opening, forbidden,
                forbidden + " forbidden per opening");

        Contains(menu,
            "RowActionMenuCancelForwarder : MonoBehaviour,",
            "one reusable keyboard and Escape forwarder");
        Contains(menu,
            ".AddComponent<RowActionMenuCancelForwarder>()",
            "focus forwarders are precreated with the shared surface");
        Contains(menu,
            "SelectMenuSurface();",
            "neutral menu surface receives initial focus");
        Contains(menu,
            "_menuPanel.gameObject);",
            "initial focus does not highlight the first action");
        Contains(menu,
            "IPointerDownHandler",
            "disabled shared actions retain an Escape target");
        Contains(menu,
            "IMoveHandler",
            "directional navigation remains available");
        Contains(menu,
            "ISubmitHandler",
            "keyboard submit enters the action list");
        Contains(menu,
            "EventSystem.current.SetSelectedGameObject(gameObject);",
            "pointer-down selects even a disabled shared action");
        Contains(menu,
            "_menuPanel.raycastTarget = false;",
            "menu padding falls through to the dismiss layer");
        Contains(menu,
            "public void OnCancel(BaseEventData eventData)",
            "selected button forwards Escape to menu close");
        DoesNotContain(menu,
            "void Update()",
            "row action surface adds no polling loop");
        Contains(menu,
            "enabled = false;",
            "closed menu disables its lifecycle component");
        Contains(menu,
            "enabled = true;",
            "open menu receives disable lifecycle callbacks");
        Contains(menu,
            "_scrollRect.onValueChanged.AddListener(HandleScrollChanged);",
            "one permanent scroll-dismiss listener");
        foreach (var movedAction in new[]
                 {
                     "Copy Edits",
                     "Paste Edits",
                     "Export UV Map",
                     "Export .obj"
                 })
            DoesNotContain(popup, movedAction,
                "global popup remains separate from row action " + movedAction);
    }

    private static void RendererAndMaterialRowsExposeOnlyTheApprovedActions()
    {
        var menu = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowActionMenu.cs");
        var rendererFactory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Renderer.cs");
        var materialFactory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.MaterialShader.cs");
        var rendererBinder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.Renderer.cs");
        var materialBinder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.MaterialShader.cs");
        var openMaterial = Slice(
            menu,
            "internal void OpenMaterial(",
            "internal void CloseIfOwner(");

        foreach (var action in new[]
                 {
                     "\"Export UV Map\"",
                     "\"Export .obj\"",
                     "\"Copy Material\"",
                     "\"Remove Material\"",
                     "\"Rename on...\""
                 })
            Contains(menu + materialBinder, action,
                "approved row action " + action);
        foreach (var forbidden in new[]
                 {
                     "Paste Material",
                     "Reset Material",
                     "Technical Info",
                     "Coming Soon"
                 })
            DoesNotContain(menu, forbidden,
                forbidden + " forbidden row action");

        Contains(rendererFactory,
            "\"RendererActionMenuButton\"",
            "one renderer action trigger");
        Contains(rendererFactory,
            "\"SelectInterpolableRendererButton\"",
            "renderer Timeline action remains direct");
        DoesNotContain(rendererFactory,
            "\"ExportUVButton\"",
            "UV export no longer consumes a direct row button");
        DoesNotContain(rendererFactory,
            "\"ExportObjButton\"",
            "OBJ export no longer consumes a direct row button");
        Equal(1,
            CountOccurrences(rendererBinder,
                "controls.ActionMenuButton,"),
            "one bound renderer menu trigger");

        Contains(materialFactory,
            "\"MaterialCollapseButton\"",
            "material fold remains direct");
        Contains(materialFactory,
            "\"MaterialText\"",
            "material name remains direct");
        Contains(materialFactory,
            "\"MaterialActionMenuButton\"",
            "one material action trigger");
        Contains(materialFactory,
            "\"MaterialCopyEditsButton\"",
            "Copy Edits is a visible primary material action");
        Contains(materialFactory,
            "\"MaterialPasteEditsButton\"",
            "Paste Edits is a visible primary material action");
        DoesNotContain(openMaterial,
            "\"Copy Edits\"",
            "Copy Edits is not hidden or duplicated in the secondary menu");
        DoesNotContain(openMaterial,
            "\"Paste Edits\"",
            "Paste Edits is not hidden or duplicated in the secondary menu");
        Equal(1,
            CountOccurrences(materialBinder,
                "controls.ActionMenuButton,"),
            "one bound material menu trigger");
        Contains(materialBinder,
            "MaterialEditorClipboardPolicy.CanPaste(",
            "Paste availability is revalidated against the visible material context");
        Contains(materialBinder,
            "controls.PasteEditsButton.interactable = canPaste;",
            "Paste stays visible and uses Selectable interactability");
        Contains(materialBinder,
            "ListenForClipboardChanges",
            "Copy refreshes visible Paste controls without rebuilding rows");
        DoesNotContain(materialBinder,
            "PasteEditsButton.gameObject.SetActive",
            "Paste is disabled rather than hidden");
        Contains(menu,
            "_tooltips[index].SetStandardTooltipText(tooltip);",
            "shared buttons preserve action-specific tooltips");
        Contains(menu,
            "_labels[row] = button.GetComponentInChildren<Text>(true);",
            "inactive shared-button labels are cached before first opening");
        DoesNotContain(openMaterial,
            "GetComponentInChildren<Text>",
            "material opening performs no label hierarchy lookup");
        Contains(menu,
            "Export the UV map of this renderer.",
            "legacy UV-export help text");
        Contains(materialFactory,
            "Copy all edits from this material",
            "legacy material-copy help text");

        var tooltipManager = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.TooltipManager.cs");
        Contains(tooltipManager,
            "Panel.transform.SetAsLastSibling();",
            "shared action tooltips render above popup surfaces");
        Contains(tooltipManager,
            "panel.raycastTarget = false;",
            "tooltip surface cannot block popup dismissal");
        Contains(tooltipManager,
            "tooltipText.raycastTarget = false;",
            "tooltip text cannot block popup dismissal");
    }

    private static void RowBindingInvalidatesEveryRetainedActionContext()
    {
        var binder = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowBinder.cs");
        var menu = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowActionMenu.cs");

        var bind = Slice(
            binder,
            "internal void Bind(RowModel item, bool force)",
            "internal void SuspendListeners()");
        Equal(true,
            bind.IndexOf("if (!force", StringComparison.Ordinal)
            < bind.IndexOf("InvalidateActionMenu();", StringComparison.Ordinal),
            "same-model fast path does not close an unchanged menu");
        Equal(true,
            bind.IndexOf("InvalidateActionMenu();", StringComparison.Ordinal)
            < bind.IndexOf("_currentModel = item;", StringComparison.Ordinal),
            "Bind invalidates before replacing the model");

        var clear = Slice(
            binder,
            "private void ClearListeners()",
            "private void InvalidateActionMenu()");
        Equal(true,
            clear.IndexOf("CloseIfOwner(this);", StringComparison.Ordinal)
            < clear.IndexOf("if (!_bindingActive)", StringComparison.Ordinal),
            "menu closes outside listener early return");
        Contains(binder,
            "internal void SuspendListeners()\n        {\n            InvalidateActionMenu();",
            "listener suspension invalidates menu");
        Contains(binder,
            "internal void Release()\n        {\n            InvalidateActionMenu();",
            "row release invalidates menu");
        Contains(binder,
            "private void OnDestroy()\n        {\n            Release();",
            "destroy uses the release invalidation path");
        Contains(binder,
            "_currentModel.Enabled",
            "invocation validates current enabled state");
        Contains(binder,
            "gameObject.activeInHierarchy",
            "invocation validates active pooled row");
        Contains(menu,
            "eventSystem.SetSelectedGameObject(focusTarget);",
            "closing menu restores or clears transient focus");
        Contains(menu,
            "Close(false);",
            "owner invalidation never restores focus to a recycled row");

        var invoke = Slice(
            menu,
            "private void Invoke(int index)",
            "private void Invoke0()");
        Equal(true,
            invoke.IndexOf("var action = _lease.Take(", StringComparison.Ordinal)
            < invoke.IndexOf("action();", StringComparison.Ordinal),
            "lease is taken before callback invocation");
        Contains(invoke,
            "Close(false);",
            "confirmed action clears focus before rebuilding rows");
        Equal(true,
            invoke.IndexOf("Close(false);", StringComparison.Ordinal)
            < invoke.IndexOf("action();", StringComparison.Ordinal),
            "visual and focus context clear before callback invocation");
    }

    private static void ExistingDirectControlsAndObjDeferralRemainIntact()
    {
        var materialFactory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.MaterialShader.cs");
        var rendererFactory = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowViewFactory.Renderer.cs");
        var presenter = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.MaterialEditorPresenter.cs");
        var ui = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.cs");
        var menu = ReadSource(
            "src", "MaterialEditor.Base", "UI", "UI.RowActionMenu.cs");

        foreach (var direct in new[]
                 {
                     "\"ShaderDropdown\"",
                     "\"ShaderResetButton\"",
                     "\"ShaderRenderQueueInput\"",
                     "\"ShaderRenderQueueResetButton\""
                 })
            Contains(materialFactory, direct,
                direct + " remains a direct control");
        Contains(rendererFactory,
            "\"RendererRecalculateNormalsToggle\"",
            "recalculate normals remains a direct toggle");
        Contains(presenter,
            "ExportObj = () => _actions.RequestObjExport(renderer)",
            "OBJ action keeps late-export request semantics");
        Contains(ui,
            "if (Session.TryTakeObjExport(out var renderer))",
            "OnGUI consumes pending OBJ request");
        DoesNotContain(menu,
            "Export.ExportObj",
            "menu never exports OBJ immediately");
    }

    private static string ReadSource(params string[] segments)
    {
        return File.ReadAllText(
                Path.Combine(new[] { FindRepositoryRoot() }.Concat(segments).ToArray()))
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');
    }

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[]
                 {
                     Directory.GetCurrentDirectory(),
                     AppContext.BaseDirectory
                 })
        {
            var directory = new DirectoryInfo(start);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(
                        directory.FullName,
                        "tests",
                        "MaterialEditor.MetadataTests",
                        "MaterialEditor.MetadataTests.csproj")))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static string Slice(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        var endIndex = source.IndexOf(
            end,
            startIndex + start.Length,
            StringComparison.Ordinal);
        if (startIndex < 0 || endIndex < 0)
            throw new InvalidOperationException("Could not isolate source contract.");
        return source.Substring(startIndex, endIndex - startIndex);
    }

    private static void Contains(string source, string value, string name)
    {
        if (!source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + " missing.");
    }

    private static void DoesNotContain(string source, string value, string name)
    {
        if (source.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException(name + " present.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
