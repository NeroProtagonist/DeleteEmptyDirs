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
                        ++argIndex;
                        if (argIndex < args.Length) {
                            root = args[argIndex];
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
            uint numFailures = 0;
            uint numReparsePoints = 0;
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

                    bool reparsePoint = false;
                    if ((dirAttributes & FileAttributes.ReparsePoint) != 0) {
                        reparsePoint = true;
                        ++numReparsePoints;
                        dirAttributes &= ~FileAttributes.ReparsePoint;
                    }
                    if (dirAttributes != 0) {
                        Console.WriteLine("Directory attributes for '{0}' unknown (TODO: handle) - exiting", dir.FullName);
                        return;
                    }

                    bool unfinishedBusiness = false;
                    int subDirCount = 0;
                    if (!reparsePoint) {
                        // Get child directories
                        IEnumerable<DirectoryInfo> subDirs = dir.EnumerateDirectories();
                        foreach (DirectoryInfo subDir in subDirs) {
                            if (!visitedDirs.Contains(subDir.FullName)) {
                                ++numToProcess;
                                searchStack.Push(subDir);
                                visitedDirs.Add(subDir.FullName);
                                unfinishedBusiness = true;
                            }
                        }
                        subDirCount = subDirs.Count();
                    }

                    if (!unfinishedBusiness || reparsePoint) {
                        // Processed all children
                        Console.SetCursorPosition(0, Math.Max(0, Console.CursorTop - 1));

                        if (reparsePoint) {
                            Console.WriteLine("Skipping reparse point '{0}'", dir.FullName);
                        } else if (subDirCount == 0 && dir.EnumerateFiles().Count() == 0) {
                            // Delete this dir
                            Console.Write("Deleting empty directory '{0}'...", dir.FullName);
                            try {
                                if (!dryRun) {
                                    dir.Delete();
                                }
                                Console.WriteLine("OK");
                                ++numDeleted;
                            } catch (Exception exception) {
                                Console.WriteLine("FAILED with exception '{0}'", exception.ToString());
                                ++numFailures;
                            }
                        }

                        searchStack.Pop();
                        ++numProcessed;
                        Console.WriteLine("Processed {0}\\{1} directories", numProcessed, numToProcess);
                    }
                }
            } else {
                Console.WriteLine("Directory '{0}' does not exist", rootDir);
            }

            Console.WriteLine("-------------------------");
            Console.WriteLine("         Summary");
            Console.WriteLine("-------------------------");
            Console.WriteLine("Successfully deleted {0} directories", numDeleted);
            Console.WriteLine("Failed to delete {0} directories", numFailures);
            Console.WriteLine("Skipped {0} linked directories", numReparsePoints);
            Console.WriteLine();
        }
    }
}
