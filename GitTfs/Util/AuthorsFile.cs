﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core;
using System.Diagnostics;

namespace Sep.Git.Tfs.Util
{
    public class Author
    {
        public string Name;
        public string Email;
    }

    [StructureMapSingleton]
    public class AuthorsFile
    {
        private readonly Dictionary<string, Author> authors = new Dictionary<string, Author>(StringComparer.OrdinalIgnoreCase);

        public AuthorsFile()
        { }


        public Dictionary<string, Author> Authors
        {
            get
            {
                return this.authors;
            }
        }

        public void Parse(TextReader authorsFileStream)
        {
            if (authorsFileStream != null)
            {
                int lineCount = 0;
                string line = authorsFileStream.ReadLine();
                while (line != null)
                {
                    lineCount++;
                    if (!line.StartsWith("#"))
                    {
                        //regex pulled from git svn script here: https://github.com/git/git/blob/master/git-svn.perl
                        Regex ex = new Regex(@"^(.+?|\(no author\))\s*=\s*(.+?)\s*<(.+)>\s*$");
                        Match match = ex.Match(line);
                        if (match.Groups.Count != 4 || String.IsNullOrWhiteSpace(match.Groups[1].Value) || String.IsNullOrWhiteSpace(match.Groups[2].Value) || String.IsNullOrWhiteSpace(match.Groups[3].Value))
                        {
                            throw new GitTfsException("Invalid format of Authors file on line " + lineCount + ".");
                        }
                        else
                        {
                            if (!authors.ContainsKey(match.Groups[1].Value))
                            {
                                //git svn doesn't trim, but maybe this should?
                                authors.Add(match.Groups[1].Value, new Author() { Name = match.Groups[2].Value, Email = match.Groups[3].Value });
                            }
                        }
                    }
                    line = authorsFileStream.ReadLine();
                }
            }
        }

        public void Parse(string authorsFilePath, string gitDir)
        {
            var savedAuthorFile = Path.Combine(gitDir, "git-tfs_authors");
            if (!String.IsNullOrWhiteSpace(authorsFilePath))
            {
                if (!File.Exists(authorsFilePath))
                {
                    throw new GitTfsException("Authors file cannot be found: '" + authorsFilePath + "'");
                }
                else
                {
                    Trace.WriteLine("Reading authors file : " + authorsFilePath);
                    using (StreamReader sr = new StreamReader(authorsFilePath))
                    {
                        Parse(sr);
                    }
                    try
                    {
                        File.Copy(authorsFilePath, savedAuthorFile, true);
                    }
                    catch (Exception) { }
                }
            }
            else if (File.Exists(savedAuthorFile))
            {
                Trace.WriteLine("Reading cached authors file (" + savedAuthorFile + ")...");
                using (StreamReader sr = new StreamReader(savedAuthorFile))
                {
                    Parse(sr);
                }
            }
            else
                Trace.WriteLine("No authors file used.");
        }
    }
}
