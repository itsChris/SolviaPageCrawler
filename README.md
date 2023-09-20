# Web Link Checker

This project provides a tool to recursively crawl a website and check for broken links, special links (like `mailto:` and `tel:`), and external links. It also handles and logs SSL certificate warnings.

## Features

- **Recursive Crawling:** Automatically browses through all internal links within the specified domain.
- **Link Validation:** Detects broken links by identifying HTTP status codes (3xx, 4xx, 5xx).
- **Special Links Detection:** Lists special links such as `mailto:` and `tel:`.
- **External Link Detection:** Lists external links without following them.
- **SSL Certificate Validation:** Checks the validity of SSL certificates and logs a warning if an invalid certificate is detected.

## Usage

1. Clone the repository:
    ```bash
    git clone https://github.com/itsChris/SolviaPageCrawler
    cd SolviaPageCrawler
    ```

2. Compile and run:
    ```bash
    dotnet run
    ```

3. Follow the on-screen prompts or logs to view the progress and results.

## Important Notes

- **Rate Limits:** This tool uses concurrent tasks to speed up the link checking process. Ensure you do not overwhelm servers or get blocked by making too many requests in a short span of time.
- **SSL Warnings:** If the tool encounters an invalid SSL certificate, it will log a red warning in the console but will continue execution.

## Contribute

Feel free to fork this repository and contribute. Pull requests are welcome!
