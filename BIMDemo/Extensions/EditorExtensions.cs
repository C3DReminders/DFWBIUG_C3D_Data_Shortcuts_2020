using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.Extensions
{
    public static class EditorExtensions
    {
        public static ObjectId GetEntityByType(this Editor ed, List<TypedValue> typeValues, string addPrompt, string removePrompt)
        {
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();

            var options = new PromptSelectionOptions()
            {
                MessageForAdding = addPrompt,
                MessageForRemoval = removePrompt,
                SelectEverythingInAperture = true,
                SingleOnly = true
            };

            var selectionFilter = new SelectionFilter(typeValues.ToArray());

            var promptResult = ed.GetSelection(options, selectionFilter);

            if (promptResult.Status == PromptStatus.OK)
            {
                return promptResult.Value.GetObjectIds().First();
            }

            return ObjectId.Null;
        }
    }
}
