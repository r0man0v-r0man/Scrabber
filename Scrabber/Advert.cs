using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabber
{
    public class Advert
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public int Price { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int Area { get; set; }
        public string Description { get; set; }
        public List<Image> Images { get; set; }
        public Advert()
        {
            Images = new List<Image>();
        }
    }
    public class Image 
    {
        public int Id { get; set; }
        public string Link { get; set; }
    }

}
