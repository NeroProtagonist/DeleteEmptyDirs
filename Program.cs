using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeleteEmptyDirs
{
    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("usage: DeleteEmptyFiles --root <rootDir> [--dry-run]");
        }

        static void Main(string[] args)
        {
            string root = "";
            bool dryRun = false;
            for (uint argIndex = 0; argIndex < args.Length; ++argIndex) {
                string op = args[argIndex];
                switch (op) {
                    case "--root": {
                        if (argIndex + 1 < args.Length) {
                            root = args[argIndex + 1];
                            ++argIndex;
                        }
                        break;
                    }
                    case "--dry-run": {
                        dryRun = true;
                        break;
                    }
                }
            }

            if (root == "") {
                PrintUsage();
                return;
            }

            DirectoryInfo rootDir = new DirectoryInfo(root);
            uint numProcessed = 0;
            uint numToProcess = 0;
            uint numDeleted = 0;
            if (rootDir.Exists) {
                Stack<DirectoryInfo> searchStack = new Stack<DirectoryInfo>();
                HashSet<string> visitedDirs = new HashSet<string>();
                ++numToProcess;
                searchStack.Push(rootDir);
                visitedDirs.Add(rootDir.FullName);
                while (searchStack.Count != 0) {
                    DirectoryInfo dir = searchStack.Peek();

                    FileAttributes dirAttributes = dir.Attributes;

                    // Remove flags that are OK to delete/process
                    dirAttributes &= ~(FileAttributes.ReadOnly
                                     | FileAttributes.Hidden
                                     | FileAttributes.System
                                     | FileAttributes.Directory
                                     | FileAttributes.Archive
                                     | FileAttributes.NotContentIndexed);
                    if (dirAttributes != 0) {
                        // TODO: Just ignore this and subdirs
                        Console.WriteLine("Directory attributes for '{0}' unknown (TODO: handle) - exiting", dir.FullName);
                        return;
                    }

                    // Get child directories
                    IEnumerable<DirectoryInfo> subDirs = dir.EnumerateDirectories();
                    bool unfinishedBusiness = false;
                    foreach (DirectoryInfo subDir in subDirs) {
                        if (!visitedDirs.Contains(subDir.FullName)) {
                            ++numToProcess;
                            searchStack.Push(subDir);
                            visitedDirs.Add(subDir.FullName);
                            unfinishedBusiness = true;
                        }
                    }

                    if (!unfinishedBusiness) {
                        // Processed all children
                        Console.SetCursorPosition(0, Math.Max(0, Console.CursorTop - 1));

                        if (subDirs.Count() == 0 && dir.EnumerateFiles().Count() == 0) {
                            // Delete this dir
                            Console.Write("Deleting directory '{0}'...", dir.FullName);
                            try {
                                if (!dryRun) {
                                    dir.Delete();
                                }
                                Console.WriteLine("OK");
                            } catch (Exception exception) {
                                Console.WriteLine("FAILED with exception '{0}'", exception.ToString());
                            }
                            ++numDeleted;
                        }

                        searchStack.Pop();
                        ++numProcessed;
                        Console.WriteLine("Processed {0}\\{1} directories", numProcessed, numToProcess);
                    }
                }
            } else {
                Console.WriteLine("Directory '{0}' does not exist", rootDir);
            }

            ++numDeleted;
        }
    }
}
