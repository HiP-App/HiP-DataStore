using System;
using System.Collections.Generic;
using System.Text;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public enum UserRole{
        Administrator,
        Supervisor,
        Student        
    }
    class User : IEntity<int>
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public int Scores { get; set; }

        public UserRole Role { get; set; }

    }
}
