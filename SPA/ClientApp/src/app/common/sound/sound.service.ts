import { Injectable } from '@angular/core';
import { AwsService } from '@sersol/ngx';

@Injectable({
    providedIn: 'root'
})
export class SoundService {

    notifySound: HTMLAudioElement;
    alertSound: HTMLAudioElement;
    completeSound: HTMLAudioElement;

    constructor(private aws: AwsService) {
        this.notifySound = new Audio(aws.getS3Url('assets/audio/message.mp3'));
        this.alertSound = new Audio(aws.getS3Url('assets/audio/alert.mp3'));
        this.completeSound = new Audio(aws.getS3Url('assets/audio/complete.mp3'));


        this.notifySound.preload = 'auto';
        this.alertSound.preload = 'auto';
        this.completeSound.preload = 'auto';
    }

    notify() {
        this.notifySound.play();
    }

    alert() {
        this.alertSound.play();
    }

    complete() {
        this.completeSound.play();
    }
}
