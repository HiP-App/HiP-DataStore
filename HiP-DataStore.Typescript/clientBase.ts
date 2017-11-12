// Sample template: Angular2

import * as generated from "./serviceClients";
import { RequestOptionsArgs } from '@angular/http'; // ignore

export class ClientBase {
    
    /**
     * The value for the HTTP Authorization header,
        e.g. "Bearer [Your JWT token here]".
     */
    public AuthorizationToken:string = "";

    protected transformOptions(options: RequestOptionsArgs) {
        options.headers.append("Authorization", this.AuthorizationToken);         
        return Promise.resolve(options);
    }
}