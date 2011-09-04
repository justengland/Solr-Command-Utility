using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;


namespace HealthGrades.Hospitals.Common {
    public static class StringExtensions {
        public static String ToWebUrl(this String str) {
            if (str == null || String.IsNullOrEmpty(str.Trim())) {
                throw new ArgumentNullException("str");
            }

            return str.Trim().ToLower().Replace(" ", "-");
        }

        public static String FromWebUrl(this String urlString) {
            if (urlString == null || String.IsNullOrEmpty(urlString.Trim())) {
                throw new ArgumentNullException("urlString");
            }
            urlString = urlString.Replace("-", " ");
            String[]partArray=urlString.Split();
            String firstLetter;
            String newUrlString = "";
            //upper cases first letter of each word in words passed in.
            for (int i = 0; i < partArray.Length; i++)
            {
               String firstLettExists = partArray[i].ToUpper() as String; //defensive coding for regEx commented out for 3 word cities
               if (!String.IsNullOrEmpty(firstLettExists))
               {
                   firstLetter = partArray[i].Substring(0, 1).ToUpper();
                   partArray[i] = partArray[i].ToString().Remove(0, 1);
                   partArray[i] = partArray[i].Insert(0, firstLetter);
                   newUrlString = String.Join(" ", partArray);
               }
             }
            

            return newUrlString;
        }
    }
}
