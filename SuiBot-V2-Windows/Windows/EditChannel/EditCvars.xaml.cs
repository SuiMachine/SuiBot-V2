using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SuiBot_V2_Windows.Windows.EditChannel
{
    /// <summary>
    /// Interaction logic for EditCvars.xaml
    /// </summary>
    public partial class EditCvars : Window
    {
        public SuiBot_Core.Storage.CustomCvars CustomCvars { get; private set; }

        public EditCvars(SuiBot_Core.Storage.CustomCvars CustomCvars)
        {
            InitializeComponent();
            this.CustomCvars = CustomCvars;
        }
    }
}
