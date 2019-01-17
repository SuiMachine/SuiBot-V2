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
    /// Interaction logic for EditQuotes.xaml
    /// </summary>
    public partial class EditQuotes : Window
    {
        public SuiBot_Core.Storage.Quotes Quotes { get; private set; }

        public EditQuotes(SuiBot_Core.Storage.Quotes Quotes)
        {
            InitializeComponent();
            this.Quotes = Quotes;
        }
    }
}
