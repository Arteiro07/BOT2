using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace QnA
{
    public class Test
    {

        
        public void Write(string t_write)
        {

            using (var file = new StreamWriter(@"C:\Users\fvsa155\Desktop\Estagio\Stats\Stats.txt", append:true))
            {
                file.WriteLine(t_write);
            }
                
        }
    }
}
