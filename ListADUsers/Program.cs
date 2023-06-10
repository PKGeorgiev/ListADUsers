using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;

/*
 * RODC Sample: Searching for user accounts, *regardless of DC type* (Writeble DC or RODC)
 * Author     : Petar Georgiev / 2014-09-18
 */

namespace ListADUsers
{
    class Program
    {

        // Generic function that returns the value of a SINGLEVALUED attribute
        private static T? GetValueOfSingleValuedAttr<T>(SearchResult sr, String propertyName)
        {
            ResultPropertyValueCollection pvc = sr.Properties[propertyName];
            return pvc.Count > 0 ? (T)Convert.ChangeType(pvc[0], typeof(T)) : default(T);
        }

        // Generic function that returns the value of a MULTIVALUED attribute
        // The values are returned as List<T>
        private static List<T> GetValueOfMultiValuedAttr<T>(SearchResult sr, String propertyName)
        {
            ResultPropertyValueCollection pvc = sr.Properties[propertyName];
            List<T> lst = new List<T>();

            foreach (T s in pvc)
            {
                lst.Add(s);
            }

            return lst;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        static void Main(string[] args)
        {
            Domain dom = Domain.GetComputerDomain();

            //  Search for a DC using LocatorOptions
            //  - ForceRediscovery : clears local DC Cache and force new DC rediscovery
            //  - WriteableRequired: the DC must be Writable. 
            //      * This should be used ONLY if you require to modify ldap objects
            //      * If you perform only searching - *DO NOT SPECIFY* this flag
            //  In our case we are searching and require ANY DC (DCs in closest site win)
            //  dcSearch: DC to be used ONLY for searching
            DomainController dcSearch = dom.FindDomainController(LocatorOptions.ForceRediscovery);


            // dcWritable: DC to be used for modifying ldap objects
            DomainController dcWritable = dom.FindDomainController(LocatorOptions.ForceRediscovery | LocatorOptions.WriteableRequired);

            Console.WriteLine("Current Machine Domain      : {0}", dom.Name);
            Console.WriteLine("Domain Controller (SEARCH)  : {0}", dcSearch.Name);
            Console.WriteLine("Domain Controller (WRITABLE): {0}", dcWritable.Name);
            
            // Get DirectorySearcher DIRECTLY from discovered DC. This ensures the searches are made directly to this DC!
            DirectorySearcher ds = dcSearch.GetDirectorySearcher();

            ds.PageSize = 1000;
            ds.SearchScope = SearchScope.Subtree;
            
            // Specify which property values to return
            ds.PropertiesToLoad.Add("samAccountName");
            ds.PropertiesToLoad.Add("displayName");
            ds.PropertiesToLoad.Add("distinguishedName");
            ds.PropertiesToLoad.Add("canonicalName");
            ds.PropertiesToLoad.Add("mail");
            ds.PropertiesToLoad.Add("UserAccountControl");

            // Searching for user accounts only: http://msdn.microsoft.com/en-us/library/ms679637(v=vs.85).aspx
            // sAMAccountType is indexed attribute so searching is faster and less resource intensive
            String ldapQuery = String.Format("(sAMAccountType=805306368)");

            Console.WriteLine("Search Root                 : {0}", ds.SearchRoot.Path);
            Console.WriteLine("Ldap Filter                 : {0}", ldapQuery);

            ds.Filter = ldapQuery;

            Console.WriteLine("\r\nPress Enter to begin searching...");
            Console.ReadLine();

            int displayCount = 10;

            foreach (SearchResult sr in ds.FindAll())
            {

                //  sr contains the values for all specified attributes. 
                //  So there's no need to create new DirectoryEntry!

                Console.WriteLine("------------------------------------------------");
                Console.WriteLine("User        : {0}", GetValueOfSingleValuedAttr<String>(sr, "samAccountName"));
                Console.WriteLine("UAC         : {0}", GetValueOfSingleValuedAttr<int>(sr, "UserAccountControl"));
                //Console.WriteLine("Display Name: {0}", GetValueOfSingleValuedAttr<String>(sr, "displayName"));
                //Console.WriteLine("E-Mail      : {0}", GetValueOfSingleValuedAttr<String>(sr, "mail"));
                //Console.WriteLine("DN          : {0}", GetValueOfSingleValuedAttr<String>(sr, "canonicalName"));

                //  NB: When using DirectoryEntry AdsPath should contain DC's fully qualified name (dns name)
                //      Format: LDAP://dc-dns-name/object-distinguished-name
                //      Using DC DNS name will ensure Kerberos is used for authentication (instead of deprecated NTLM)


                // If you need to get the LDAP ENTRY (DirectoryEntry) then supply correct DC name (for RO and RW operatioin)

                // Construct ADS path for the ReadOnly entry
                String strObjectRO = String.Format("LDAP://{0}/{1}", dcSearch.Name, GetValueOfSingleValuedAttr<String>(sr, "distinguishedName"));
                
                // Construct ADS path for the Read/Write entry
                String strObjectRW = String.Format("LDAP://{0}/{1}", dcWritable, GetValueOfSingleValuedAttr<String>(sr, "distinguishedName"));

                // Console.WriteLine("ADS PATH RO : {0}", strObjectRO);
                // Console.WriteLine("ADS PATH RW : {0}", strObjectRW);

                // Get ReadOnly Entry. Modify it's properties will fail because RODC does not permit changes
                DirectoryEntry objectRO = new DirectoryEntry(strObjectRO);
                
                // Query object's properties for objectRO here

                // Get Read/Write Entry. Modify it's properties will succeed
                // DirectoryEntry objectRW = new DirectoryEntry(strObjectRW);
                // Modify the object and commit changes for objectRW here

                displayCount--;

                if (displayCount < 0)
                    break;
            }

            Console.WriteLine("\r\nThat's it! Press Enter to exit...");
            Console.ReadLine();


        }
    }
}
