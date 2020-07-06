using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SecureMemo.Utility
{
    public static class ControlHelper
    {
        public static Control GetChildControlByName(Control parent, string name)
        {
            Control userControl = null;
            foreach (Control control in parent.Controls)
            {
                if (control.Name == name)
                {
                    userControl = control;
                    break;
                }

                userControl = GetChildControlByName(control, name);
            }

            return userControl;
        }

        public static IEnumerable<Control> GetChildControlByType(this Control parent, Type controlType)
        {
            foreach (Control control in parent.Controls)
                if (control.GetType() == controlType)
                    yield return control;
                else
                    yield return control.GetChildControlByType(controlType).FirstOrDefault();

            yield return null;
        }
    }
}