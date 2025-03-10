import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';

export type MyRecord = { Id: string, Title: string };

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
  protected data = signal<MyRecord[]>([]);

  private _httpClient = inject(HttpClient);

  onStartHttpClient() {
    this.streamWithHttpClient().subscribe((data) => {
      this.data.set([...this.data(), data]);
    });
  }

  onStartStream() {
    this.streamWithFecth().subscribe((data) => {
      this.data.set([...this.data(), data]);
    });
  }

  private streamWithHttpClient(): Observable<MyRecord> {
    return new Observable<MyRecord>(observer => {
      this._httpClient.get("/stream", { responseType: 'text' }).subscribe(response => {
        const reader = new ReadableStreamDefaultReader(new Response(response).body!);
        const gen = createStream(reader);
        (async () => {
          while (true) {
            const { done, value } = await gen.next();
            if (done) {
              observer.complete();
              return;
            }
            observer.next(value);
          }
        })();
      });
    });
  }

  private streamWithFecth(): Observable<MyRecord> {
    return new Observable<MyRecord>(observer => {
      fetch("/stream").then(res => {
        const reader = res.body!.getReader();
        const gen = createStream(reader);
        (async () => {
          while (true) {
            const { done, value } = await gen.next();
            if (done) {
              observer.complete();
              return;
            }
            observer.next(value);
          }
        })();
      });
    });
  }
}

export async function* createStream(reader: ReadableStreamDefaultReader): AsyncGenerator<MyRecord, void> {
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();

    if (done) {
      if (buffer.length > 0) {
        yield JSON.parse(buffer);
      }
      return;
    }

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split(/\r?\n/);
    buffer = lines.pop()!;

    for (const line of lines) {
      yield JSON.parse(line);
    }
  }
}
