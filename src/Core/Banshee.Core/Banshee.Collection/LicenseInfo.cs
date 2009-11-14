//
// LicenseInfo.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

namespace CreativeCommons
{
    public class LicenseInfo
    {
    }

    [Flags]
    public enum Permissions
    {
        None = 0,
        Reproduction,
        Distribution,
        DerivativeWorks,
        HighIncomNationUse,
        Sharing
    }

    [Flags]
    public enum Requirements
    {
        None = 0,
        Notice,
        Attribution,
        ShareAlike,
        SourceCode
    }

    [Flags]
    public enum Prohibitions
    {
        None = 0,
        CommercialUse
    }

    public class CreativeCommonsLicenseInfo
    {
        private static string [] known_licenses = new string [] {
            "http://creativecommons.org/licenses/by/2.5/rdf",
            "http://creativecommons.org/licenses/by/3.0/rdf",
        };

        public static IEnumerable<CreativeCommonsLicenseInfo> KnownLicenses {
            get {
                foreach (string known_license in known_licenses) {
                    if (!licenses.ContainsKey (known_license)) {
                        FromLicenseUrl (known_license);
                    }
                }

                foreach (CreativeCommonsLicenseInfo license in licenses.Values) {
                    if (license != null)
                        yield return license;
                }
            }
        }

        private Permissions permissions;
        private Requirements requirements;
        private Prohibitions prohibitions;

        private bool is_deprecated;
        private string title;
        private string version;
        private string code_name;
        private string about_url;
        private string legal_url;
        private string jurisdiction_url;
        private string creator_url;

        /*public static CreateiveCommonsLicenseInfo FromCodeName (string code_name)
        {
            return null;
        }*/

        private static Dictionary<string, CreativeCommonsLicenseInfo> licenses = new Dictionary<string, CreativeCommonsLicenseInfo> ();

        public static CreateiveCommonsLicenseInfo FromLicenseUrl (string license_url)
        {
            if (String.IsNullOrEmpty (license_url))
                return null;

            string rdf_url = GetRdfUrl (license_url);
            if (rdf_url == null)
                return null;

            if (licenses.ContainsKey (rdf_url)) {
                return licenses[rdf_url];
            }

            CreativeCommonsLicenseInfo license = null; // Grab from web and parse
            licenses[rdf_url] = license;
            return license;
        }

        private static string GetRdfUrl (string license_url)
        {
            if (license_uri.StartsWith ("http://creativecommons.org/licenses/")) {
                if (license_url.EndsWith ("/rdf"))
                    return license_url;
                else if (license_url.EndsWith ("/"))
                    return String.Format ("{0}rdf", license_url);
                else
                    return String.Format ("{0}/rdf", license_url);
            }
            return null;
        }


        public LicenseInfo ()
        {
        }

        /*public void GetLicenseInfo (string license_uri)
        {
            if (String.IsNullOrEmpty (license_uri))
                return;

            if (license_uri.StartsWith ("http://creativecommons.org/")) {
                if ()
            }
        }*/
    }

    public class CreativeCommonsRdfParser
    {
        public static CreativeCommonsLicenseInfo (string xml_blob)
        {
        }

        //ns_mgr = XmlUtils.GetNamespaceManager (doc);
        //ns_mgr.AddNamespace ("itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd");
        /*xmlns:cc='http://creativecommons.org/ns#'
        xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'
        xmlns:dc='http://purl.org/dc/elements/1.1/'
        xmlns:dcq='http://purl.org/dc/terms/'*/
    }
}

/*
<?xml version="1.0" encoding="utf-8"?>
<rdf:RDF
  xmlns:cc='http://creativecommons.org/ns#'
  xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'
  xmlns:dc='http://purl.org/dc/elements/1.1/'
  xmlns:dcq='http://purl.org/dc/terms/'
>
  <cc:License rdf:about="http://creativecommons.org/licenses/by-sa/3.0/us/">
    <dcq:hasVersion>3.0</dcq:hasVersion>
    <cc:requires rdf:resource="http://creativecommons.org/ns#ShareAlike"/>
    <cc:requires rdf:resource="http://creativecommons.org/ns#Attribution"/>
    <cc:requires rdf:resource="http://creativecommons.org/ns#Notice"/>
    <dc:creator rdf:resource="http://creativecommons.org"/>
    <cc:legalcode rdf:resource="http://creativecommons.org/licenses/by-sa/3.0/us/legalcode"/>

    <dc:title xml:lang="ca">Reconeixement-CompartirIgual</dc:title>
    <dc:title xml:lang="hu">Nevezd meg! - Így add tovább!</dc:title>
    <dc:title xml:lang="ko">저작자표시-동일조건변경허락</dc:title>
    <dc:title xml:lang="fi">Nimeä-Tarttuva</dc:title>
    <dc:title xml:lang="es-cl">Atribución-LicenciarIgual</dc:title>
    <dc:title xml:lang="en-ca">Attribution-ShareAlike</dc:title>

    <dc:title xml:lang="es-mx">Atribución-Licenciamiento Recíproco</dc:title>
    <dc:title xml:lang="es-pe">Reconocimiento-CompartirIgual</dc:title>
    <dc:title xml:lang="pt-pt">Atribuição-Partilha nos termos da mesma licença</dc:title>
    <dc:title xml:lang="da">Navngivelse-DelPåSammeVilkår</dc:title>
    <dc:title xml:lang="en">Attribution-ShareAlike</dc:title>
    <dc:title xml:lang="en-gb">Attribution-ShareAlike</dc:title>

    <dc:title xml:lang="sv">Erkännande-DelaLika</dc:title>
    <dc:title xml:lang="pt">Atribuição-Compartilhamento pela mesma licença</dc:title>
    <dc:title xml:lang="de">Namensnennung-Weitergabe unter gleichen Bedingungen</dc:title>
    <dc:title xml:lang="st">Attribution-ShareAlike</dc:title>
    <dc:title xml:lang="pl">Uznanie autorstwa-Na tych samych warunkach</dc:title>
    <dc:title xml:lang="it-ch">Attribuzione - Condividi allo stesso modo</dc:title>

    <dc:title xml:lang="af">Erkenning-InsgelyksDeel</dc:title>
    <dc:title xml:lang="it">Attribuzione - Condividi allo stesso modo</dc:title>
    <dc:title xml:lang="fr-lu">Paternité - Partage des Conditions Initiales à l'Identique</dc:title>
    <dc:title xml:lang="fr-ch">Paternité - Partage des Conditions Initiales à l'Identique</dc:title>
    <dc:title xml:lang="eo">Atribuo-distribui samrajte</dc:title>
    <dc:title xml:lang="en-us">Attribution-ShareAlike</dc:title>

    <dc:title xml:lang="bg">Признание-Споделяне на споделеното</dc:title>
    <dc:title xml:lang="fr">Paternité - Partage des Conditions Initiales à l'Identique</dc:title>
    <dc:title xml:lang="nso">Tsebagatšo -Mohlakanelwa</dc:title>
    <dc:title xml:lang="gl">Recoñecemento-CompartirIgual</dc:title>
    <dc:title xml:lang="eu">Aitortu-PartekatuBerdin</dc:title>
    <dc:title xml:lang="es">Reconocimiento-CompartirIgual</dc:title>

    <dc:title xml:lang="sl">Priznanje avtorstva-Deljenje pod enakimi pogoji</dc:title>
    <dc:title xml:lang="es-co">Reconocimiento-CompartirIgual</dc:title>
    <dc:title xml:lang="ja">表示 - 継承</dc:title>
    <dc:title xml:lang="es-ar">Atribución-CompartirDerivadasIgual</dc:title>
    <dc:title xml:lang="fr-ca">Paternité - Partage des Conditions Initiales à l'Identique</dc:title>
    <dc:title xml:lang="nl">Naamsvermelding-GelijkDelen</dc:title>

    <dc:title xml:lang="de-ch">Namensnennung-Weitergabe unter gleichen Bedingungen</dc:title>
    <dc:title xml:lang="zh-tw">姓名標示-相同方式分享</dc:title>
    <dc:title xml:lang="mk">НаведиИзвор-СподелиПодИстиУслови</dc:title>
    <dc:title xml:lang="zh">署名-相同方式共享</dc:title>
    <dc:title xml:lang="zu">Qaphela Umnikazi-Zihlanganyeleni</dc:title>
    <dc:title xml:lang="he">ייחוס-שיתוף זהה</dc:title>

    <dc:title xml:lang="ms">Pengiktirafan-PerkongsianSerupa</dc:title>
    <dc:title xml:lang="hr">Imenovanje-Dijeli pod istim uvjetima</dc:title>
    <dc:title xml:lang="de-at">Namensnennung-Weitergabe unter gleichen Bedingungen</dc:title>
    <cc:jurisdiction rdf:resource="http://creativecommons.org/international/us/"/>
    <cc:permits rdf:resource="http://creativecommons.org/ns#Reproduction"/>
    <cc:permits rdf:resource="http://creativecommons.org/ns#DerivativeWorks"/>
    <cc:permits rdf:resource="http://creativecommons.org/ns#Distribution"/>

    <dc:source rdf:resource="http://creativecommons.org/licenses/by-sa/3.0/"/>
  </cc:License>
</rdf:RDF>
*/