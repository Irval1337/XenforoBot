using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XenforoBot
{
    public class Group
    {
        public int groupId { get; set; }
        public ulong dsRoleId { get; set; }
    }

    public class SettingsJSON
    {
        public string dsToken { get; set; }
        public string xfToken { get; set; }
        public int[] groupsHierarchy { get; set; }
        public Group[] groups { get; set; }
    }
}
