using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace WebUploaderDemo.Controllers
{
    public class UploadController : Controller
    {
        private readonly string UploadPath = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(),
                "UploadFiles/files/"));
        
        public IActionResult Index()
        {
            return View();
        }


        public int[] GetUploadedChunk(string fileName, string md5, int chunkSize)
        {
            var sourcePath = Path.Combine(UploadPath, "chunk\\" + md5 + "\\");
            
            var dicInfo = new DirectoryInfo(sourcePath);
            if (Directory.Exists(Path.GetDirectoryName(sourcePath)))
            {
                var files = dicInfo.GetFiles();
                return files.Select(s => int.Parse(s.Name)).ToArray();
            }

            return new int[0];
        }





        [HttpPost]
        public string CheckChunk(string fileName, string md5, int chunk, int chunkSize)
        {
            var filePath = UploadPath + "chunk\\" + md5 + "\\" + chunk;
            if (System.IO.File.Exists(filePath))
            {
                return JsonConvert.SerializeObject(new
                {
                    exist = true 
                });
            }
            else
            {
                return JsonConvert.SerializeObject(new
                {
                    exist = false 
                });
            }
        }

        [HttpGet]
        public string GetMaxChunk(string md5, string ext)
        {
            try
            {
                var chunk = 0;
                var fileName = md5 + "." + ext;
                var file = new FileInfo(UploadPath + fileName);
                if (file.Exists)
                {
                    chunk = Int32.MaxValue;
                }
                else
                {
                    if (Directory.Exists(UploadPath + "chunk\\" + md5))
                    {
                        DirectoryInfo dicInfo = new DirectoryInfo(UploadPath + "chunk\\" + md5);
                        var files = dicInfo.GetFiles();
                        chunk = files.Length;
                        if (chunk > 1)
                        {
                            chunk = chunk - 1;
                        }

                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    code = 0,
                    errMsg = string.Empty,
                    data = chunk
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    code = 0,
                    errMsg = string.Empty,
                    data = 0
                });

            }
        }

        public string Upload(IFormFile file)
        {
            
            var md5_key = $"{Request.Form["id"]}md5";
            var md5_val = Request.Form[md5_key];
            if (Request.Form.Keys.Any(m => m == "chunk"))
            {
                //取得chunk和chunks
                var chunk = Convert.ToInt32(Request.Form["chunk"]);//当前分片在上传分片中的顺序（从0开始）
                var chunks = Convert.ToInt32(Request.Form["chunks"]);//总分片数

                var folder = UploadPath + "chunk\\" + md5_val + "\\";
                var path = folder + chunk;
                if (!Directory.Exists(Path.GetDirectoryName(folder)))
                {
                    Directory.CreateDirectory(folder);
                }

                FileStream addFile = null;
                BinaryWriter addWiriter = null;
                Stream stream = null;
                BinaryReader tempReader = null;

                try
                {
                    if (System.IO.File.Exists(path))
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            code = 0,
                            errMsg = string.Empty,
                            data = "{\"chunked\" : true, \"ext\" : \"" + Path.GetExtension(file.FileName) + "\"}"
                        });
                    }

                    addFile = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                    addWiriter = new BinaryWriter(addFile);

                    stream = file.OpenReadStream();
                    tempReader = new BinaryReader(stream);

                    addWiriter.Write(tempReader.ReadBytes((int) stream.Length));
                }
                catch (Exception e)
                {
                }
                finally
                {
                    if (addFile != null)
                    {
                        addFile.Close();
                        addFile.Dispose();
                    }

                    if (addWiriter != null)
                    {
                        addWiriter.Close();
                        addWiriter.Dispose();
                    }

                    if (stream != null)
                    {
                        stream.Close();
                        stream.Dispose();
                    }

                    if (tempReader != null)
                    {
                        tempReader.Close();
                        tempReader.Dispose();
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    code = 0,
                    errMsg = string.Empty,
                    data = "{\"chunked\" : true, \"ext\" : \"" + Path.GetExtension(file.FileName) + "\"}"
                });
            }
            else
            {
                var path = UploadPath + md5_val + Path.GetExtension(file.FileName);
                //file.CopyTo();
                using (var fileStream = System.IO.File.Create(path))
                {
                    file.CopyTo(fileStream);
                }

                return JsonConvert.SerializeObject(new
                {
                    code = 0,
                    errMsg = string.Empty,
                    data = "{\"chunked\" : false}"
                });
            }
        }

        public string MergeFiles()
        {
            var guid = Request.Form["md5"];
            var ext = Request.Form["ext"];
            var sourcePath = Path.Combine(UploadPath, "chunk\\" + guid + "\\");
            var targetPath = Path.Combine(UploadPath, guid + ext);

            var dicInfo = new DirectoryInfo(sourcePath);
            if (Directory.Exists(Path.GetDirectoryName(sourcePath)))
            {
                var files = dicInfo.GetFiles();
                foreach (var file in files.OrderBy(f => int.Parse(f.Name)))
                {
                    var addFile = new FileStream(targetPath, FileMode.Append, FileAccess.Write);
                    var addWriter = new BinaryWriter(addFile);

                    var stream = file.Open(FileMode.Open);
                    var tempReader = new BinaryReader(stream);
                    addWriter.Write(tempReader.ReadBytes((int) stream.Length));
                    tempReader.Close();
                    stream.Close();
                    addWriter.Close();
                    addFile.Close();

                    tempReader.Dispose();
                    stream.Dispose();
                    addWriter.Dispose();
                    addFile.Dispose();
                }
                DeleteFolder(sourcePath);
                return JsonConvert.SerializeObject(new
                {
                    code = 0,
                    errMsg = string.Empty,
                    data = "{\"chunked\" : true, \"hasError\" : false, \"savePath\" :\"" +
                           WebUtility.UrlEncode(targetPath) + "\"}"
                });
            }
            else
            {
                return JsonConvert.SerializeObject(new
                {
                    code = 0,
                    errMsg = string.Empty,
                    data = "{\"hasError\" : true}"
                });
            }
        }

        private void DeleteFolder(string path)
        {
            //删除这个目录下的所有子目录
            if (Directory.GetDirectories(path).Length > 0)
            {
                foreach (var fl in Directory.GetDirectories(path))
                {
                    Directory.Delete(fl, true);
                }
            }
            //删除这个目录下的所有文件
            if (Directory.GetFiles(path).Length > 0)
            {
                foreach (var f in Directory.GetFiles(path))
                {
                    System.IO.File.Delete(f);
                }
            }
            Directory.Delete(path, true);
        }

        [HttpPost]
        public string AddUploadRecord()
        {
            //...
            return JsonConvert.SerializeObject(new
            {
                code = 0,
                errmsg = string.Empty,
                data = string.Empty
            });
        }
    }
}