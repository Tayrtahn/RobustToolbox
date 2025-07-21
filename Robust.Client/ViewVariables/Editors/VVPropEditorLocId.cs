using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Editors;

internal sealed class VVPropEditorLocId : VVPropEditor
{
    private readonly ILocalizationManager _loc;

    public VVPropEditorLocId()
    {
        _loc = IoCManager.Resolve<ILocalizationManager>();
    }

    protected override Control MakeUI(object? value)
    {
        var castValue = (LocId)(value ?? "");
        var idEdit = new LineEdit
        {
            Text = castValue,
            Editable = !ReadOnly,
            HorizontalExpand = true,
        };

        if (!ReadOnly)
        {
            idEdit.OnTextEntered += e =>
            {
                ValueChanged((LocId)e.Text);
            };
        }

        var idContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Children =
            {
                new Label()
                {
                    Text = _loc.GetString("vv-locid-id"),
                },
                idEdit,
            }
        };

        var preview = new LineEdit
        {
            Editable = false,
            HorizontalExpand = true,
        };
        UpdatePreview(preview, value);

        OnValueChanged += (value, _) =>
        {
            UpdatePreview(preview, value);
        };

        var previewContainer = new BoxContainer()
        {
            Orientation = LayoutOrientation.Horizontal,
            Children =
            {
                new Label()
                {
                    Text = _loc.GetString("vv-locid-preview"),
                },
                preview,
            }
        };

        var vBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Children =
            {
                idContainer,
                previewContainer,
            }
        };
        return vBox;
    }

    private void UpdatePreview(LineEdit preview, object? value)
    {
        var castValue = (LocId)(value ?? "");
        preview.SetText(_loc.TryGetString(castValue, out var localized) ? localized : _loc.GetString("vv-locid-missing"));
    }
}
