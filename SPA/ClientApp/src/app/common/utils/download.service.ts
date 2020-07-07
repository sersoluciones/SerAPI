import { Injectable } from '@angular/core';

/**
 * @description Servicio para descargar archivos mediante una url en distintas situaciones
 */
@Injectable({
  providedIn: 'root'
})
export class DownloadService {

    downloadElement: any;

  constructor() {
      this.downloadElement = document.createElement('a');
      this.downloadElement.setAttribute('target', '_blank');
      document.body.appendChild(this.downloadElement);
  }

  /**
   * @description Descarga tradicional mediante un a[href] emulando un clic de usuario
   * @param url Url absoluta
   */
  get(url: string) {
      this.downloadElement.setAttribute('href', url);
      this.downloadElement.click();
  }
}
