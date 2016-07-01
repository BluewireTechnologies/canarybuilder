using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Async;
using Bluewire.Common.GitWrapper.Parsing.Diff;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.UnitTests.Parsing
{
    [TestFixture]
    public class GitDiffReaderTests
    {
        private readonly Regex rxLineBreaks = new Regex("\r?\n");

        private IEnumerator<string> EnumerateLines()
        {
            foreach(var line in rxLineBreaks.Split(diff)) yield return line;
        }

        private async Task<GitDiffReader.DiffLine[]> CollectChunkLines(GitDiffReader reader)
        {
            var lines = new List<GitDiffReader.DiffLine>();
            while(await reader.NextLine())
            {
                lines.Add(reader.Line);
            }
            return lines.ToArray();
        }

        [Test]
        public async Task ParsesFirstFileHeader()
        {
            using (var lines = EnumerateLines().ToAsync())
            {
                var reader = new GitDiffReader(lines);

                Assert.True(await reader.NextFile());
                Assert.That(reader.Path, Is.EqualTo("Bluewire.Epro.Web.Dtos/Drugs/DrugSearchQueryDto.cs"));
                Assert.That(reader.OriginalPath, Is.EqualTo("Bluewire.Epro.Web.Dtos/Drugs/DrugSearchQueryDto.cs"));
            }
        }

        [Test]
        public async Task CallingNextFileAgainWillSkipToTheSecondFile()
        {
            using (var lines = EnumerateLines().ToAsync())
            {
                var reader = new GitDiffReader(lines);

                Assert.True(await reader.NextFile());
                Assert.True(await reader.NextFile());
                Assert.That(reader.Path, Is.EqualTo("Epro/Poplist/Drugs/DrugSearchQuery.cs"));
                Assert.That(reader.OriginalPath, Is.EqualTo("Epro/Poplist/Drugs/DrugSearchQuery.cs"));
            }
        }

        [Test]
        public async Task CanReadChunkWithDeletions()
        {
            using (var lines = EnumerateLines().ToAsync())
            {
                var reader = new GitDiffReader(lines);

                Assert.True(await reader.NextFile());
                Assert.True(await reader.NextChunk());

                var chunkLines = await CollectChunkLines(reader);

                Assert.That(chunkLines, Is.EqualTo(new [] {
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber = 10, NewNumber = 10, Text = "        public string Search { get; set; }" },
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber = 11, NewNumber = 11, Text = "        public int Terms { get; set; }" },
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber = 12, NewNumber = 12, Text = "" },
                    new GitDiffReader.DiffLine { Action = LineAction.Delete , OldNumber = 13, NewNumber =  0, Text = "        public string RawSearch { get; set; }" },
                    new GitDiffReader.DiffLine { Action = LineAction.Delete , OldNumber = 14, NewNumber =  0, Text = "" },
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber = 15, NewNumber = 13, Text = "        public string VtmId { get; set; }" },
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber = 16, NewNumber = 14, Text = "        public string VmpId { get; set; }" },
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber = 17, NewNumber = 15, Text = "        public string AmpId { get; set; }" },
                }));
            }
        }
        

        [Test]
        public async Task CanReadChunkWithDeletionsAndInsertions()
        {
            using (var lines = EnumerateLines().ToAsync())
            {
                var reader = new GitDiffReader(lines);

                Assert.True(await reader.NextFile());
                Assert.True(await reader.NextFile());
                Assert.True(await reader.NextFile());
                Assert.True(await reader.NextFile());
                Assert.True(await reader.NextChunk());

                var chunkLines = await CollectChunkLines(reader);

                Assert.That(chunkLines, Is.EqualTo(new [] {
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber =  8, NewNumber =  8, Text = "            search : search," },
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber =  9, NewNumber =  9, Text = "            searchNoPrefix : search," },
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber = 10, NewNumber = 10, Text = "            prefix : null," },
                    new GitDiffReader.DiffLine { Action = LineAction.Delete , OldNumber = 11, NewNumber =  0, Text = "            length : search.length," },
                    new GitDiffReader.DiffLine { Action = LineAction.Delete , OldNumber = 12, NewNumber =  0, Text = "            rawSearch : rawSearch" },
                    new GitDiffReader.DiffLine { Action = LineAction.Insert , OldNumber =  0, NewNumber = 11, Text = "            length : search.length" },
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber = 13, NewNumber = 12, Text = "        };" },
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber = 14, NewNumber = 13, Text = "        if(behaviour.prefixMatch)" },
                    new GitDiffReader.DiffLine { Action = LineAction.Context, OldNumber = 15, NewNumber = 14, Text = "        {" },
                }));
            }
        }

        [Test]
        public async Task CanReadFileHeadersUntilEOF()
        {
            using (var lines = EnumerateLines().ToAsync())
            {
                var reader = new GitDiffReader(lines);

                while(await reader.NextFile()) { }
            }
        }

        [Test]
        public async Task CanReadChunkHeadersUntilEOF()
        {
            using (var lines = EnumerateLines().ToAsync())
            {
                var reader = new GitDiffReader(lines);

                await reader.NextFile();
                await reader.NextFile();
                await reader.NextFile();
                Assert.True(await reader.NextFile());
                while(await reader.NextChunk()) { }
            }
        }

        [Test]
        public async Task CanReadLinesUntilEOF()
        {
            using (var lines = EnumerateLines().ToAsync())
            {
                var reader = new GitDiffReader(lines);

                await reader.NextFile();
                await reader.NextFile();
                await reader.NextFile();
                Assert.True(await reader.NextFile());
                Assert.True(await reader.NextChunk());
                while(await reader.NextLine()) { }
            }
        }

        // 4 files, 1 chunk per file
        private string diff =
@"diff --git a/Bluewire.Epro.Web.Dtos/Drugs/DrugSearchQueryDto.cs b/Bluewire.Epro.Web.Dtos/Drugs/DrugSearchQueryDto.cs
index 386d037..45b3a01 100644
--- a/Bluewire.Epro.Web.Dtos/Drugs/DrugSearchQueryDto.cs
+++ b/Bluewire.Epro.Web.Dtos/Drugs/DrugSearchQueryDto.cs
@@ -10,8 +10,6 @@ public class DrugSearchQueryDto
         public string Search { get; set; }
         public int Terms { get; set; }
 
-        public string RawSearch { get; set; }
-
         public string VtmId { get; set; }
         public string VmpId { get; set; }
         public string AmpId { get; set; }
diff --git a/Epro/Poplist/Drugs/DrugSearchQuery.cs b/Epro/Poplist/Drugs/DrugSearchQuery.cs
index 3af0dc7..9f465d1 100644
--- a/Epro/Poplist/Drugs/DrugSearchQuery.cs
+++ b/Epro/Poplist/Drugs/DrugSearchQuery.cs
@@ -19,13 +19,6 @@ public DrugSearchQuery()
 
         public int Terms { get; set; }
 
-        private string _rawSearch;
-        public string RawSearch
-        {
-            get { return _rawSearch ?? Search; }
-            set { _rawSearch = value; }
-        }
-
         public string VtmId { get; set; }
         public string VmpId { get; set; }
         public string AmpId { get; set; }
diff --git a/Web/Scripts/Lib/Poplist/Behaviours/Drugs.js b/Web/Scripts/Lib/Poplist/Behaviours/Drugs.js
index 1cd2415..ecc705a 100644
--- a/Web/Scripts/Lib/Poplist/Behaviours/Drugs.js
+++ b/Web/Scripts/Lib/Poplist/Behaviours/Drugs.js
@@ -101,7 +101,6 @@ PoplistBehaviours.Drugs = {
                     data : {
                         searchJson : JSON.stringify({
                             search : query.search,
-                            rawSearch : query.rawSearch,
                             vtmId : poplistControl.behaviour.vtm && poplistControl.behaviour.vtm.id,
                             includeOrderSentences : poplistControl.behaviour.isForPatientPrescription,
                             showOnlyPrescribableResults : poplistControl.behaviour.isForPatientPrescription && !showAllResults,
diff --git a/Web/Scripts/Lib/Poplist/DefaultQueryParser.js b/Web/Scripts/Lib/Poplist/DefaultQueryParser.js
index a584634..321b794 100644
--- a/Web/Scripts/Lib/Poplist/DefaultQueryParser.js
+++ b/Web/Scripts/Lib/Poplist/DefaultQueryParser.js
@@ -8,8 +8,7 @@
             search : search,
             searchNoPrefix : search,
             prefix : null,
-            length : search.length,
-            rawSearch : rawSearch
+            length : search.length
         };
         if(behaviour.prefixMatch)
         {
";
    }
}
