using System;
using System.Diagnostics;
using System.Text;
using Azure.Storage.Blobs;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lucene.Net.Store.Azure.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        private readonly string _connectionString;

        public IntegrationTests() : this(null) { }

        public IntegrationTests(string connectionString)
        {
            _connectionString = connectionString;
        }

        [TestMethod]
        public void TestReadAndWrite()
        {

            var connectionString = _connectionString ?? "UseDevelopmentStorage=true";
            const string containerName = "testcatalog";
            var blobClient = new BlobServiceClient(connectionString);
            var container = blobClient.GetBlobContainerClient(containerName);
            container.DeleteIfExists();

            var azureDirectory = new AzureDirectory(connectionString, containerName);

            var (dog, cat, car) = InitializeCatalog(azureDirectory, 1000);

            try
            {

                var ireader = DirectoryReader.Open(azureDirectory);
                for (var i = 0; i < 100; i++)
                {
                    var searcher = new IndexSearcher(ireader);
                    var searchForPhrase = SearchForPhrase(searcher, "dog");
                    Assert.AreEqual(dog, searchForPhrase);
                    searchForPhrase = SearchForPhrase(searcher, "cat");
                    Assert.AreEqual(cat, searchForPhrase);
                    searchForPhrase = SearchForPhrase(searcher, "car");
                    Assert.AreEqual(car, searchForPhrase);
                }
                Trace.TraceInformation("Tests passsed");
            }
            catch (Exception x)
            {
                Trace.TraceInformation("Tests failed:\n{0}", x);
            }
            finally
            {
                // check the container exists, and delete it
                Assert.IsTrue(container.Exists()); // check the container exists
                container.Delete();
            }
        }

        [TestMethod]
        public void TestReadAndWriteWithSubDirectory()
        {
            var connectionString = _connectionString ?? "UseDevelopmentStorage=true";
            const string containerName = "testcatalog";
            var blobClient = new BlobServiceClient(connectionString);
            var container = blobClient.GetBlobContainerClient(containerName);
            container.DeleteIfExists();

            var azureDirectory = new AzureDirectory(connectionString, $"{containerName}/subdirectory");

            var (dog, cat, car) = InitializeCatalog(azureDirectory, 1000);

            try
            {

                var ireader = DirectoryReader.Open(azureDirectory);
                for (var i = 0; i < 100; i++)
                {
                    var searcher = new IndexSearcher(ireader);
                    var searchForPhrase = SearchForPhrase(searcher, "dog");
                    Assert.AreEqual(dog, searchForPhrase);
                    searchForPhrase = SearchForPhrase(searcher, "cat");
                    Assert.AreEqual(cat, searchForPhrase);
                    searchForPhrase = SearchForPhrase(searcher, "car");
                    Assert.AreEqual(car, searchForPhrase);
                }
                Trace.TraceInformation("Tests passsed");
            }
            catch (Exception x)
            {
                Trace.TraceInformation("Tests failed:\n{0}", x);
            }
            finally
            {
                // check the container exists, and delete it
                Assert.IsTrue(container.Exists()); // check the container exists
                container.Delete();
            }
        }
        
        [TestMethod]
        public void TestReadAndWriteWithTwoShardDirectories()
        {
            var connectionString = _connectionString ?? "UseDevelopmentStorage=true";
            const string containerName = "testcatalogwithshards";
            var blobClient = new BlobServiceClient(connectionString);
            var container = blobClient.GetBlobContainerClient(containerName);
            container.DeleteIfExists();

            var azureDirectory1 = new AzureDirectory(connectionString, $"{containerName}/shard1");
            var (dog, cat, car) = InitializeCatalog(azureDirectory1, 1000);
            var azureDirectory2 = new AzureDirectory(connectionString, $"{containerName}/shard2");
            var (dog2, cat2, car2) = InitializeCatalog(azureDirectory2, 500);

            ValidateDirectory(azureDirectory1, dog, cat, car);
            ValidateDirectory(azureDirectory2, dog2, cat2, car2);

            // delete all azureDirectory1 blobs
            foreach (string file in azureDirectory1.ListAll())
            {
                azureDirectory1.DeleteFile(file);
            }

            ValidateDirectory(azureDirectory2, dog2, cat2, car2);

            foreach (string file in azureDirectory2.ListAll())
            {
                azureDirectory2.DeleteFile(file);
            }
        }
        
        [TestMethod]
        public void TestReadAndWrite_WritingTwoConsecutiveTimes()
        {

            var connectionString = _connectionString ?? "UseDevelopmentStorage=true";
            const string containerName = "testcatalog";
            var blobClient = new BlobServiceClient(connectionString);
            var container = blobClient.GetBlobContainerClient(containerName);
            container.DeleteIfExists();

            var azureDirectory = new AzureDirectory(connectionString, containerName);

            var (dog, cat, car) = InitializeCatalog(azureDirectory, 500);
            var (dog1, cat1, car1) = InitializeCatalog(azureDirectory, 500);
            dog += dog1;
            cat += cat1;
            car += car1;

            try
            {
                var ireader = DirectoryReader.Open(azureDirectory);
                for (var i = 0; i < 100; i++)
                {
                    var searcher = new IndexSearcher(ireader);
                    var searchForPhrase = SearchForPhrase(searcher, "dog");
                    Assert.AreEqual(dog, searchForPhrase);
                    searchForPhrase = SearchForPhrase(searcher, "cat");
                    Assert.AreEqual(cat, searchForPhrase);
                    searchForPhrase = SearchForPhrase(searcher, "car");
                    Assert.AreEqual(car, searchForPhrase);
                }
                Trace.TraceInformation("Tests passsed");
            }
            catch (Exception x)
            {
                Trace.TraceInformation("Tests failed:\n{0}", x);
            }
            finally
            {
                // check the container exists, and delete it
                Assert.IsTrue(container.Exists()); // check the container exists
                container.Delete();
            }
        }

        [TestMethod]
        public void TestReadAndWriteWithSubDirectory_WritingTwoConsecutiveTimes()
        {
            var connectionString = _connectionString ?? "UseDevelopmentStorage=true";
            const string containerName = "testcatalog";
            var blobClient = new BlobServiceClient(connectionString);
            var container = blobClient.GetBlobContainerClient(containerName);
            container.DeleteIfExists();

            var azureDirectory = new AzureDirectory(connectionString, $"{containerName}/subdirectory");

            var (dog, cat, car) = InitializeCatalog(azureDirectory, 500);
            var (dog1, cat1, car1) = InitializeCatalog(azureDirectory, 500);
            dog += dog1;
            cat += cat1;
            car += car1;

            try
            {

                var ireader = DirectoryReader.Open(azureDirectory);
                for (var i = 0; i < 100; i++)
                {
                    var searcher = new IndexSearcher(ireader);
                    var searchForPhrase = SearchForPhrase(searcher, "dog");
                    Assert.AreEqual(dog, searchForPhrase);
                    searchForPhrase = SearchForPhrase(searcher, "cat");
                    Assert.AreEqual(cat, searchForPhrase);
                    searchForPhrase = SearchForPhrase(searcher, "car");
                    Assert.AreEqual(car, searchForPhrase);
                }
                Trace.TraceInformation("Tests passsed");
            }
            catch (Exception x)
            {
                Trace.TraceInformation("Tests failed:\n{0}", x);
            }
            finally
            {
                // check the container exists, and delete it
                Assert.IsTrue(container.Exists()); // check the container exists
                container.Delete();
            }
        }
        
        [TestMethod]
        public void TestReadAndWriteWithTwoShardDirectories_WritingTwoConsecutiveTimes()
        {
            var connectionString = _connectionString ?? "UseDevelopmentStorage=true";
            const string containerName = "testcatalogwithshards";
            var blobClient = new BlobServiceClient(connectionString);
            var container = blobClient.GetBlobContainerClient(containerName);
            container.DeleteIfExists();

            var azureDirectory1 = new AzureDirectory(connectionString, $"{containerName}/shard1");
            var (dog, cat, car) = InitializeCatalog(azureDirectory1, 500);
            var (dog1, cat1, car1) = InitializeCatalog(azureDirectory1, 500);
            dog += dog1;
            cat += cat1;
            car += car1;
            
            var azureDirectory2 = new AzureDirectory(connectionString, $"{containerName}/shard2");
            var (dog2, cat2, car2) = InitializeCatalog(azureDirectory2, 250);
            var (dog3, cat3, car3) = InitializeCatalog(azureDirectory2, 250);
            dog2 += dog3;
            cat2 += cat3;
            car2 += car3;

            ValidateDirectory(azureDirectory1, dog, cat, car);
            ValidateDirectory(azureDirectory2, dog2, cat2, car2);

            // delete all azureDirectory1 blobs
            foreach (string file in azureDirectory1.ListAll())
            {
                azureDirectory1.DeleteFile(file);
            }

            ValidateDirectory(azureDirectory2, dog2, cat2, car2);

            foreach (string file in azureDirectory2.ListAll())
            {
                azureDirectory2.DeleteFile(file);
            }
        }

        [TestMethod]
        public void CanListAllFileNames_InFlatContainer()
        {
            // Arrange
            var expectedFileNames = string.Join("\n", new[]
            {
                "_0.cfe",
                "_0.cfs",
                "_0.si",
                "segments.gen",
                "segments_1",
                "write.lock"
            });
            TestListingFilesOfDirectory("testcatalog", expectedFileNames);
        }
        
        [TestMethod]
        public void CanListAllFileNames_InLevel1Subdirectory()
        {
            // Arrange
            var expectedFileNames = string.Join("\n", new[]
            {
                "_0.cfe",
                "_0.cfs",
                "_0.si",
                "segments.gen",
                "segments_1",
                "write.lock"
            });
            TestListingFilesOfDirectory("testcatalog/shard1", expectedFileNames);
        }
        
        [TestMethod]
        public void CanListAllFileNames_InLevel2Subdirectory()
        {
            // Arrange
            var expectedFileNames = string.Join("\n", new[]
            {
                "_0.cfe",
                "_0.cfs",
                "_0.si",
                "segments.gen",
                "segments_1",
                "write.lock"
            });
            TestListingFilesOfDirectory("testcatalog/shard1/level2", expectedFileNames);
        }
        
        [TestMethod]
        public void CanListAllFileNames_InFlatContainer_After2Writes()
        {
            // Arrange
            var expectedFileNames = string.Join("\n", new[]
            {
               "_0.cfe",
               "_0.cfs",
               "_0.si",
               "_1.cfe",
               "_1.cfs",
               "_1.si",
               "segments_2",
               "write.lock",
            });
            TestListingFilesOfDirectory("testcatalog", expectedFileNames, numberOfSimulatedIndexWrites:2);
        }
        
        [TestMethod]
        public void CanListAllFileNames_InLevel1Subdirectory_After2Writes()
        {
            // Arrange
            var expectedFileNames = string.Join("\n", new[]
            {
                "_0.cfe",
                "_0.cfs",
                "_0.si",
                "_1.cfe",
                "_1.cfs",
                "_1.si",
                "segments_2",
                "write.lock",
            });
            TestListingFilesOfDirectory("testcatalog/shard1", expectedFileNames, numberOfSimulatedIndexWrites:2);
        }
        
        [TestMethod]
        public void CanListAllFileNames_InLevel2Subdirectory_After2Writes()
        {
            // Arrange
            var expectedFileNames = string.Join("\n", new[]
            {
                "_0.cfe",
                "_0.cfs",
                "_0.si",
                "_1.cfe",
                "_1.cfs",
                "_1.si",
                "segments_2",
                "write.lock",
            });
            TestListingFilesOfDirectory("testcatalog/shard1/level2", expectedFileNames, numberOfSimulatedIndexWrites:2);
        }

        private void TestListingFilesOfDirectory(string containerName, string expectedFileNames, int numberOfSimulatedIndexWrites = 1)
        {
            var connectionString = _connectionString ?? "UseDevelopmentStorage=true";
            var blobClient = new BlobServiceClient(connectionString);
            var container = blobClient.GetBlobContainerClient(containerName);
            container.DeleteIfExists();

            var azureDirectory = new AzureDirectory(connectionString, containerName);

            for (int i = 0; i < numberOfSimulatedIndexWrites; i++)
            {
                InitializeCatalog(azureDirectory, 1000/numberOfSimulatedIndexWrites);
            }
            
            // Act
            var actual = azureDirectory.ListAll();

            // Assert
            var actualFileNames = string.Join("\n", actual);
            Assert.AreEqual(expectedFileNames, actualFileNames);
        }

        private static void ValidateDirectory(AzureDirectory azureDirectory2, Int32 dog2, Int32 cat2, Int32 car2)
        {
            System.Diagnostics.Debug.WriteLine("--------- DirectoryReader ---------");
            var ireader = DirectoryReader.Open(azureDirectory2);
            for (var i = 0; i < 100; i++)
            {
                var searcher = new IndexSearcher(ireader);
                var searchForPhrase = SearchForPhrase(searcher, "dog");
                Assert.AreEqual(dog2, searchForPhrase);
                searchForPhrase = SearchForPhrase(searcher, "cat");
                Assert.AreEqual(cat2, searchForPhrase);
                searchForPhrase = SearchForPhrase(searcher, "car");
                Assert.AreEqual(car2, searchForPhrase);
            }
            Trace.TraceInformation("Tests passsed");
        }

        private static (int dog, int cat, int car) InitializeCatalog(AzureDirectory azureDirectory, int docs)
        {
            var indexWriterConfig = new IndexWriterConfig(
                Lucene.Net.Util.LuceneVersion.LUCENE_48,
                new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48));

            var dog = 0;
            var cat = 0;
            var car = 0;
            using (var indexWriter = new IndexWriter(azureDirectory, indexWriterConfig))
            {

                for (var iDoc = 0; iDoc < docs; iDoc++)
                {
                    var bodyText = GeneratePhrase(40);
                    var doc = new Document {
                        new TextField("id", DateTime.Now.ToFileTimeUtc() + "-" + iDoc, Field.Store.YES),
                        new TextField("Title", GeneratePhrase(10), Field.Store.YES),
                        new TextField("Body", bodyText, Field.Store.YES)
                    };
                    dog += bodyText.Contains(" dog ") ? 1 : 0;
                    cat += bodyText.Contains(" cat ") ? 1 : 0;
                    car += bodyText.Contains(" car ") ? 1 : 0;
                    indexWriter.AddDocument(doc);
                }

                Trace.TraceInformation("Total docs is {0}, {1} dog, {2} cat, {3} car", indexWriter.NumDocs, dog, cat, car);
            }

            return (dog, cat, car);
        }

        private static int SearchForPhrase(IndexSearcher searcher, string phrase)
        {
            var parser = new Lucene.Net.QueryParsers.Classic.QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, "Body", new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48));
            var query = parser.Parse(phrase);
            var topDocs = searcher.Search(query, 100);
            return topDocs.TotalHits;
        }

        private static readonly Random Random = new Random();

        private static readonly string[] SampleTerms = {
            "dog", "cat", "car", "horse", "door", "tree", "chair", "microsoft", "apple", "adobe", "google", "golf",
            "linux", "windows", "firefox", "mouse", "hornet", "monkey", "giraffe", "computer", "monitor",
            "steve", "fred", "lili", "albert", "tom", "shane", "gerald", "chris",
            "love", "hate", "scared", "fast", "slow", "new", "old"
        };

        private static string GeneratePhrase(int maxTerms)
        {
            var phrase = new StringBuilder();
            var nWords = 2 + Random.Next(maxTerms);
            for (var i = 0; i < nWords; i++)
            {
                phrase.AppendFormat(" {0} {1}", SampleTerms[Random.Next(SampleTerms.Length)],
                                    Random.Next(32768).ToString());
            }
            return phrase.ToString();
        }

    }
}
