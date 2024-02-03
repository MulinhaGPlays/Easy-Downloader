using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppDownloader
{
    // Classe para acompanhar o progresso da solicitação
    public class ProgressMessageHandler : DelegatingHandler
    {
        public ProgressMessageHandler() : base(new HttpClientHandler())
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var progressContent = new ProgressStreamContent(await base.SendAsync(request, cancellationToken).Result.Content.ReadAsStreamAsync(), cancellationToken);

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = progressContent
            };
        }
    }

    // Classe para monitorar o progresso da leitura ou gravação de um fluxo
    public class ProgressStreamContent : StreamContent
    {
        private readonly System.Threading.CancellationToken _cancellationToken;

        public ProgressStreamContent(System.IO.Stream content, System.Threading.CancellationToken cancellationToken) : base(content)
        {
            _cancellationToken = cancellationToken;
        }

        protected override async Task SerializeToStreamAsync(System.IO.Stream stream, TransportContext context)
        {
            var progressStream = new ProgressStream(await ReadAsStreamAsync(), _cancellationToken);
            await progressStream.CopyToAsync(stream);
        }
    }

    // Classe para monitorar o progresso da leitura ou gravação de um fluxo
    public class ProgressStream : System.IO.Stream
    {
        private readonly System.IO.Stream _originalStream;
        private readonly System.Threading.CancellationToken _cancellationToken;

        public ProgressStream(System.IO.Stream originalStream, System.Threading.CancellationToken cancellationToken)
        {
            _originalStream = originalStream ?? throw new ArgumentNullException(nameof(originalStream));
            _cancellationToken = cancellationToken;
        }

        public override bool CanRead => _originalStream.CanRead;

        public override bool CanSeek => _originalStream.CanSeek;

        public override bool CanWrite => _originalStream.CanWrite;

        public override long Length => _originalStream.Length;

        public override long Position { get => _originalStream.Position; set => _originalStream.Position = value; }

        public override void Flush()
        {
            _originalStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _originalStream.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            int bytesRead = await _originalStream.ReadAsync(buffer, offset, count, cancellationToken);

            // Aqui, você pode adicionar lógica para acompanhar o progresso
            // No exemplo, eu apenas imprimo o número de bytes lidos
            Console.WriteLine($"Lidos: {bytesRead} bytes");

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _originalStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _originalStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _originalStream.Write(buffer, offset, count);
        }

        // Implementação restante dos métodos da classe Stream (pode ser necessário, dependendo do uso)
    }
}
