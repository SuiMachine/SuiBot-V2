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
    /// Interaction logic for EditIntervalMessages.xaml
    /// </summary>
    public partial class EditIntervalMessages : Window
    {
        public SuiBot_Core.Storage.IntervalMessages IntervalMessages { get; private set; }

        public EditIntervalMessages(SuiBot_Core.Storage.IntervalMessages IntervalMessages)
        {
            InitializeComponent();
            this.IntervalMessages = IntervalMessages;
        }
    }
}
