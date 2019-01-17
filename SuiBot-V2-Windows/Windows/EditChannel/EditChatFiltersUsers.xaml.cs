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
    /// Interaction logic for EditChatFiltersUsers.xaml
    /// </summary>
    public partial class EditChatFiltersUsers : Window
    {
        public SuiBot_Core.Storage.ChatFilterUsersDB ChatFilterUsersDB { get; private set; }

        public EditChatFiltersUsers(SuiBot_Core.Storage.ChatFilterUsersDB chatFilterUsersDB)
        {
            InitializeComponent();
            this.ChatFilterUsersDB = ChatFilterUsersDB;
        }
    }
}
