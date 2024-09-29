(() => {
	document.addEventListener("DOMContentLoaded", () => {
		const audioPlayer = document.getElementById('audioPlayer');
		const audioPlayerButton = document.getElementById('audioPlayerButton');
		const audioPlayerButtonPlay = document.getElementById('audioPlayerButtonPlay');
		if (audioPlayer) {
			if (audioPlayerButton) {
				audioPlayerButton.addEventListener("click", () => {
					if (audioPlayerButtonPlay) {
						audioPlayerButtonPlay.setAttribute("class", "fa-solid fa-spinner fa-spin");
					}
					audioPlayer.style.pointerEvents = "none";
					audioPlayer.style.opacity = "0.5";

					let element = document.getElementById("textToConvert");
					const serviceRequest = {
						Text: element.value
					};

					fetch('/Text2Speech/ConvertText2Speech', {
						method: 'POST',
						headers: {
							'Content-Type': 'application/json'
						},
						body: JSON.stringify(serviceRequest)
					})
						.then(response => response.blob())
						.then(blob => {
							var url = URL.createObjectURL(blob);
							audioPlayer.src = url;
							audioPlayer.load();
							if (audioPlayer.classList.contains("d-none")) {
								audioPlayer.classList.remove("d-none");
							}
							if (!audioPlayerButton.classList.contains("d-none")) {
								audioPlayerButton.classList.add("d-none");
							}
							audioPlayer.play();
							audioPlayer.style.pointerEvents = "";
							audioPlayer.style.opacity = "";

						})
						.catch(error => {
							console.error('Error:', error);
							audioPlayer.style.pointerEvents = "";
							audioPlayer.style.opacity = "";
						});
				})
			}
		}
	});

	window.addEventListener("beforeunload", () => {
		if (hasSpeechSynth && window.speechSynthesis.speaking) {
			window.speechSynthesis.pause();
			synthPlayIcon.setAttribute("class", "fas fa-play");
		}
	})

	window.addEventListener("unload", () => {
		if (hasSpeechSynth && window.speechSynthesis.speaking) {
			window.speechSynthesis.pause();
			synthPlayIcon.setAttribute("class", "fas fa-play");
		}
	})

	window.addEventListener("pagehide", () => {
		if (hasSpeechSynth && window.speechSynthesis.speaking) {
			window.speechSynthesis.pause();
			synthPlayIcon.setAttribute("class", "fas fa-play");
		}
	})
})();