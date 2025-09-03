import { HttpClient, HttpDownloadProgressEvent, HttpEvent, HttpEventType } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { filter, interval, map, mergeMap, Observable, of, scan, startWith, switchMap, tap, throwError } from 'rxjs';

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

  onFillData(): void {
    this._httpClient.post("/stream", null).subscribe();
  }

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

  trigger = signal(false);

  streamResource = rxResource({
    params: () => ({ trigger: this.trigger() }),
    stream: ({ params }) => {
      if (params.trigger) {
        return this.streamWithHttpClientWithEvents();
      } else {
        return of();
      }
    }
  });

  onStartWithRxResourceEvents(){
    this.trigger.set(true);
  }

  onStartWithHttpClientEvents() {
    this.streamWithHttpClientWithEvents().subscribe(record => {
      // Update signal with new records
      this.data.set([...this.data(), record]);
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

  private streamWithHttpClientWithEvents(): Observable<MyRecord> {
    return this._httpClient.get("/stream", {
      observe: 'events',
      responseType: 'text',
      reportProgress: true
    }).pipe(
      // Filter only DownloadProgress events
      filter(event => event.type === HttpEventType.DownloadProgress),
      // Scan the events to accumulate the downloaded records
      scan((acc, event: HttpEvent<string>) => {
        const partialText = (event as HttpDownloadProgressEvent).partialText ?? '';
        const newContent = partialText.substring(acc.lastLoaded);
        acc.lastLoaded += newContent.length;
        const lines = newContent.split(/\r?\n/);
        acc.records = lines.filter(line => line).map(line => JSON.parse(line));
        return acc;
      }, { lastLoaded: 0, records: [] as MyRecord[] }),
      // Map the accumulated records to the output
      map(acc => acc.records),
      // Filter out empty records
      filter(records => records.length > 0),
      // Merge the records into a single stream
      mergeMap(records => records)
    );
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
