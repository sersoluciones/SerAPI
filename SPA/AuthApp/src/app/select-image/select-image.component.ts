import { Component, OnInit, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { NgxFileDropEntry } from 'ngx-file-drop';
import { ImageCroppedEvent, Dimensions } from 'ngx-image-cropper';
import { OptionsSelectImage } from './OptionsSelectImage';

@Component({
  templateUrl: './select-image.component.html',
  styleUrls: ['./select-image.component.scss']
})
export class SelectImageComponent implements OnInit {

  files: NgxFileDropEntry[] = [];

  // Variables para recortar la imagen
  imageChangedEvent: any = '';
  croppedImage: any = '';

  constructor(private dialog: MatDialogRef<SelectImageComponent>, @Inject(MAT_DIALOG_DATA) public data: OptionsSelectImage) { }

  cancel(): void {
    this.dialog.close();
  }

  ngOnInit(): void {
    if (this.data.round == null) {
      this.data.round = false;
    }
    if (this.data.file != null) {
      this.imageChangedEvent = { target: { files: [this.data.file] } };
    }
  }

  // Funciones para cuando se recorta la imagen

  imageCropped(event: ImageCroppedEvent) {
    this.croppedImage = event.base64;
  }

  imageLoaded() {
    console.log('Image loaded');
  }

  cropperReady(sourceImageDimensions: Dimensions) {
    console.log('Cropper ready', sourceImageDimensions);
  }

  loadImageFailed() {
    console.log('Load failed');
  }

}
